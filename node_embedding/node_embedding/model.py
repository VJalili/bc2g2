import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers


"""
## Model

The MPNN model can take on various shapes and forms. In this tutorial, we will implement an
MPNN based on the original paper
[Neural Message Passing for Quantum Chemistry](https://arxiv.org/abs/1704.01212) and
[DeepChem's MPNNModel](https://deepchem.readthedocs.io/en/latest/api_reference/models.html#mpnnmodel).
The MPNN of this tutorial consists of three stages: message passing, readout and
classification.

### Message passing

The message passing step itself consists of two parts:

1. The *edge network*, which passes messages from 1-hop neighbors `w^{t}_{i}` of `v^{t}`
to `v^{t}`, based on the edge features between them (`e_{v^{t}w^{t}_{i}}`, where `t =
0`), resulting in an updated node state `v^{t+1}`. `_{i}` denotes the `i:th` neighbor of
`v^{t}` and `^{t}` the `t:th` state of `v` or `w`. An important feature of the edge
network (in contrast to e.g. the relational graph convolutional network) is that it
allows for non-discrete edge features. However, in this tutorial, only discrete edge
features will be used.

2. The *gated recurrent unit* (GRU), which takes as input the most recent node state
(e.g., `v^{t+1}`) and updates it based on previous node state(s) (e.g., `v^{t}`). In
other words, the most recent node states serves as the input to the GRU, while the previous
node state(s) are incorporated within the memory state of the GRU.

Importantly, step (1) and (2) are repeated for `k steps`, and where at each step `1...k`,
the radius (or # hops) of aggregated information from the source node `v` increases by 1.
"""


class EdgeNetwork(layers.Layer):
    def __init__(self, **kwargs):
        super().__init__(**kwargs)
        # Node Features Count (nfc) & Edge Features Count (efc)
        self.nfc, self.efc = None, None
        self.kernel, self.bias, self.built = None, None, None

    def build(self, input_shape):
        self.nfc = input_shape[0][-1]
        self.efc = input_shape[1][-1]

        self.kernel = self.add_weight(
            "kernel",
            shape=(self.efc, self.nfc * self.nfc),
            trainable=True,
            initializer="glorot_uniform")

        self.bias = self.add_weight(
            "bias",
            shape=(self.nfc * self.nfc),
            trainable=True,
            initializer="zeros")

        self.built = True

    def call(self, inputs):
        node_features, edge_features, pair_indices = inputs
        edge_features = tf.matmul(edge_features, self.kernel) + self.bias
        edge_features = tf.reshape(edge_features, (-1, self.nfc, self.nfc))

        node_features_neighbors = tf.gather(node_features, pair_indices[:, 1])
        node_features_neighbors = tf.expand_dims(node_features_neighbors, axis=-1)

        # Neighborhood aggregation
        transformed_features = tf.matmul(edge_features, node_features_neighbors)

        transformed_features = tf.squeeze(transformed_features, axis=-1)
        aggregated_features = tf.math.segment_sum(transformed_features, pair_indices[:, 0])

        # aggregated_features = tf.math.unsorted_segment_sum(transformed_features, pair_indices[:, 0])
        return aggregated_features


class MessagePassing(layers.Layer):
    def __init__(self, units, steps=4, **kwargs):
        super().__init__(**kwargs)
        self.units = units
        self.steps = steps
        self.nfc = None
        self.message_step = None
        self.pad_length = None
        self.update_step = None
        self.built = None

    def build(self, input_shape):
        self.nfc = input_shape[0][-1]
        self.message_step = EdgeNetwork()
        self.pad_length = max(0, self.units - self.nfc)
        self.update_step = layers.GRUCell(self.nfc + self.pad_length)
        self.built = True

    def call(self, inputs):
        node_features, edge_features, pair_indices = inputs
        node_features_updated = tf.pad(node_features, [(0, 0), (0, self.pad_length)])
        # Aggregate node features from neighbors.
        for i in range(self.steps):
            node_features_aggregated = self.message_step(
                [node_features_updated, edge_features, pair_indices])

            node_features_updated, _ = self.update_step(
                node_features_aggregated, node_features_updated)

        return node_features_updated


"""
### Readout

When the message passing procedure ends, the k-step-aggregated node states are to be partitioned
into subgraphs (correspoding to each molecule in the batch) and subsequently
reduced to graph-level embeddings. In the
[original paper](https://arxiv.org/abs/1704.01212), a
[set-to-set layer](https://arxiv.org/abs/1511.06391) was used for this purpose.
In this tutorial however, a transformer encoder will be used. Specifically:

* the k-step-aggregated node states will be partitioned into the subgraphs
(corresponding to each molecule in the batch);
* each subgraph will then be padded to match the subgraph with the greatest number of nodes, followed
by a `tf.stack(...)`;
* the (stacked) padded tensor, encoding subgraphs (each subgraph containing sets of node states), are
masked to make sure the paddings don't interfere with training;
* finally, the padded tensor is passed to the transformer followed by an average pooling.
"""


class PartitionPadding(layers.Layer):
    def __init__(self, batch_size, **kwargs):
        super().__init__(**kwargs)
        self.batch_size = batch_size

    def call(self, inputs):
        node_features, node_partition_indices = inputs

        node_features = tf.dynamic_partition(
            node_features, node_partition_indices, self.batch_size)

        # Pad and stack sub-graphs
        num_nodes = [tf.shape(f)[0] for f in node_features]
        max_num_atoms = tf.reduce_max(num_nodes)
        node_features_padded = tf.stack(
            [
                tf.pad(f, [(0, max_num_atoms - n), (0, 0)])
                for f, n in zip(node_features, num_nodes)
            ],
            axis=0)

        # Remove empty sub-graphs (usually for last batch)
        nonempty_examples = tf.where(tf.reduce_sum(node_features_padded, (1, 2)) != 0)
        nonempty_examples = tf.squeeze(nonempty_examples, axis=-1)

        return tf.gather(node_features_padded, nonempty_examples, axis=0)


class TransformerEncoder(layers.Layer):
    def __init__(self, num_heads=8, embed_dim=64, dense_dim=512, **kwargs):
        super().__init__(**kwargs)

        self.attention = layers.MultiHeadAttention(num_heads, embed_dim)
        self.dense_proj = keras.Sequential(
            [layers.Dense(dense_dim, activation="relu"), layers.Dense(embed_dim)])
        self.layer_normalization_1 = layers.LayerNormalization()
        self.layer_normalization_2 = layers.LayerNormalization()
        self.supports_masking = True

    def call(self, inputs, mask=None):
        attention_mask = mask[:, tf.newaxis, :] if mask is not None else None
        attention_output = self.attention(inputs, inputs, attention_mask=attention_mask)
        proj_input = self.layer_normalization_1(inputs + attention_output)
        return self.layer_normalization_2(proj_input + self.dense_proj(proj_input))


class BlockChainGraphModel:

    @staticmethod
    def get_model(
            node_features_count,
            edge_features_count,
            batch_size=32,
            message_units=64,
            message_steps=4,
            num_attention_heads=8,
            dense_units=512):

        node_features = layers.Input(node_features_count, dtype="float32", name="node_features")
        edge_features = layers.Input(edge_features_count, dtype="float32", name="edge_features")
        pair_indices = layers.Input(2, dtype="int32", name="pair_indices")
        node_partition_indices = layers.Input((), dtype="int32", name="node_partition_indices")

        x = MessagePassing(message_units, message_steps)(
            [node_features, edge_features, pair_indices])

        x = PartitionPadding(batch_size)([x, node_partition_indices])

        x = layers.Masking()(x)

        x = TransformerEncoder(num_attention_heads, message_units, dense_units)(x)

        x = layers.GlobalAveragePooling1D()(x)
        before_last = layers.Dense(dense_units, activation="relu")(x)
        x = layers.Dense(1, activation="sigmoid")(before_last)

        _inputs = [node_features, edge_features, pair_indices, node_partition_indices]
        _embeder_output = before_last
        model = keras.Model(
            inputs=[node_features, edge_features, pair_indices, node_partition_indices],
            outputs=[x],
        )
        return model, _inputs, _embeder_output
