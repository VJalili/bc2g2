import h5py
import math
import numpy as np

import tensorflow as tf
from tensorflow.keras.utils import Sequence


RAND_SEED = 1


class GraphGenerator(Sequence):

    def __init__(self, filename: str, indices: list[int], batch_size=16, shuffle=True, to_fit=True):
        self.filename = filename
        self.indices = indices
        self.batch_size = batch_size
        self.shuffle = shuffle
        self.to_fit = to_fit

    def __len__(self):
        return math.ceil(len(self.indices) / self.batch_size)

    def __getitem__(self, idx):
        batch_indices = self.indices[idx * self.batch_size:(idx + 1) * self.batch_size]
        return self._get_graphs(batch_indices)

    def _get_graphs(self, indices):
        node_features, edge_features, pair_indices, labels = [], [], [], []
        with h5py.File(self.filename, "r") as f:
            for idx in indices:
                for group_key in ["graph", "random_edges"]:
                    group = f[idx][group_key]
                    node_features.append(group["node_features"][...])
                    edge_features.append(group["edge_features"][...])
                    pair_indices.append(group["pair_indices"][...])
                    if self.to_fit:
                        labels.append(group["labels"][...])
        # NOTE: while it may seem a good practice to define the above strings
        # as enums or constants, it needs careful consideration since if
        # generator depends on mutable variables it may lead to multiple
        # invocations at runtime. Do not replace these with a variable unless
        # tested extensively.

        x = (tf.ragged.constant(node_features, dtype=tf.float32),
             tf.ragged.constant(edge_features, dtype=tf.float32),
             tf.ragged.constant(pair_indices, dtype=tf.int64))

        if self.to_fit:
            y = tf.ragged.constant(labels, dtype=tf.int64)
            return self._transform(x, y)
        else:
            return x

    @staticmethod
    def _transform(x_batch, y_batch):
        node_features, edge_features, pair_indices = x_batch

        nodes_count = node_features.row_lengths()
        edges_count = edge_features.row_lengths()

        # Get partition indices.
        # node_partition_indices will be used to gather sub-graphs from
        # global graph in the model later.
        graph_indices = tf.range(len(nodes_count))
        node_partition_indices = tf.repeat(graph_indices, nodes_count)
        edge_partition_indices = tf.repeat(graph_indices[:-1], edges_count[1:])

        # Merge sub-graphs into a global graph.
        increment = tf.cumsum(nodes_count[:-1])
        increment = tf.pad(tf.gather(increment, edge_partition_indices), [(edges_count[0], 0)])
        pair_indices = pair_indices.merge_dims(outer_axis=0, inner_axis=1).to_tensor()
        pair_indices = pair_indices + increment[:, tf.newaxis]
        node_features = node_features.merge_dims(outer_axis=0, inner_axis=1).to_tensor()
        edge_features = edge_features.merge_dims(outer_axis=0, inner_axis=1).to_tensor()

        return (node_features, edge_features, pair_indices, node_partition_indices), y_batch

    def on_epoch_end(self):
        if self.shuffle:
            np.random.shuffle(self.indices)


# This section uses TF Dataset instead of the generator.
# def graph_generator():
#     with h5py.File(graphs_hdf5, "r") as f:
#         for idx in train_index:
#             node_features, edge_features, pair_indices, labels = [], [], [], []
#             for group_key in ["graph", "random_edges"]:
#                 group = f[idx][group_key]
#                 node_features.append(group["node_features"][...])
#                 edge_features.append(group["edge_features"][...])
#                 pair_indices.append(group["pair_indices"][...])
#                 labels.append(group["labels"][...])
#             x = (tf.ragged.constant(node_features, dtype=tf.float32),
#                  tf.ragged.constant(edge_features, dtype=tf.float32),
#                  tf.ragged.constant(pair_indices, dtype=tf.int32))
#             yield (x, labels)
#
# def transform(x_batch, y_batch):
#     pass
#
#
# def get_graph_dataset(filename, indices, batch_size=16, shuffle=False):
#     # generator = GraphGenerator(filename, indices, batch_size=batch_size)
#     dataset = tf.data.Dataset.from_generator(graph_generator)
#     if shuffle:
#         dataset = dataset.shuffle(RAND_SEED)
#     return dataset.batch(batch_size).map(transform, -1)


# TODO: this is a legacy approach, update to current latest best practices.
np.random.seed(RAND_SEED)

graphs_hdf5 = "C:\\Users\\Hamed\\Desktop\\code\\bitcoin_data\\node_embedding\\graph_sampling\\sampled_graphs.hdf5"
with h5py.File(graphs_hdf5, "r") as f:
    idxs = list(f.keys())
permuted_indices = np.random.permutation(idxs)

pi1, pi2 = int(len(permuted_indices) * 0.8), int(len(permuted_indices) * 0.99)
train_index = permuted_indices[: pi1]
valid_index = permuted_indices[pi1: pi2]
test_index = permuted_indices[pi2:]

# TEST
# z = get_graph_dataset(graphs_hdf5, train_index)

training_generator = GraphGenerator(graphs_hdf5, train_index)
validation_generator = GraphGenerator(graphs_hdf5, valid_index)


xyz = GraphGenerator(graphs_hdf5, train_index)
# print(xyz._get_graphs(train_index))

import model

mpnn, __inputs, __embedder_output = model.MPNNModel(
    atom_dim=1, bond_dim=4,
)



mpnn.compile(
    loss=tf.keras.losses.BinaryCrossentropy(),
    optimizer=tf.keras.optimizers.Adam(learning_rate=5e-4),
    metrics=[tf.keras.metrics.AUC(name="AUC")],
)

history = mpnn.fit(
    training_generator,
    validation_data=validation_generator,
    epochs=10,#40,
    verbose=2,
    class_weight={0: 2.0, 1: 0.5},
)