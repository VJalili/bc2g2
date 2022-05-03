import h5py
import logging
import math
import model
import numpy as np
import pandas as pd
import warnings

import tensorflow as tf
from tensorflow.keras.utils import Sequence


# TODO: Globally set the random seed, currently it is set in different places to different values.
# TODO: also add a flag that disables fixing the seed at deploy.
RAND_SEED = 1
tf.random.set_seed(RAND_SEED)
# TODO: this is a legacy approach, update to current latest best practices.
np.random.seed(RAND_SEED)

tf.get_logger().setLevel(logging.ERROR)
# TODO: disable suppressing the warnings and address them to the extend possible.
warnings.filterwarnings("ignore")


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


class GraphEncoder:
    def __init__(self, filename):
        self.filename = filename

    def train(self, train_history_filename, eval_history_filename, epochs=100, learning_rate=5e-4):
        with h5py.File(self.filename, "r") as f:
            indices = list(f.keys())
        permuted_indices = np.random.permutation(indices)

        pi1, pi2 = int(len(permuted_indices) * 0.8), int(len(permuted_indices) * 0.99)
        train_index = permuted_indices[: pi1]
        valid_index = permuted_indices[pi1: pi2]
        test_index = permuted_indices[pi2:]

        train_data_generator = GraphGenerator(self.filename, train_index)
        val_data_generator = GraphGenerator(self.filename, valid_index)
        eval_data_generator = GraphGenerator(self.filename, test_index)

        bcgm, __inputs, __embedder_output = model.BlockChainGraphModel.get_model(
            node_features_count=1, edge_features_count=4)

        bcgm.compile(
            loss=tf.keras.losses.BinaryCrossentropy(),
            optimizer=tf.keras.optimizers.Adam(learning_rate=learning_rate),
            metrics=[
                tf.keras.metrics.AUC(),
                tf.keras.metrics.Accuracy(),
                tf.keras.metrics.KLDivergence(),
                tf.keras.metrics.MeanAbsoluteError(),
                tf.keras.metrics.Precision(),
                tf.keras.metrics.Recall()])

        train_history = bcgm.fit(
            train_data_generator,
            validation_data=val_data_generator,
            epochs=epochs,
            verbose=2,
            class_weight={0: 2.0, 1: 0.5})  # hence the loss is a weighted average
        self.serialize_history(train_history.history, train_history_filename)

        eval_history = bcgm.evaluate(eval_data_generator, return_dict=True)
        self.serialize_history(eval_history, eval_history_filename)

    @staticmethod
    def serialize_history(history, filename):
        df = pd.DataFrame.from_dict(history, orient="index").transpose()
        df.to_csv(filename, sep="\t")


def main(filename, train_history_filename, eval_history_filename):
    encoder = GraphEncoder(filename)
    encoder.train(train_history_filename, eval_history_filename, epochs=2)


if __name__ == "__main__":
    hdf5_filename = "C:\\Users\\Hamed\\Desktop\\code\\bitcoin_data\\node_embedding\\graph_sampling\\sampled_graphs.hdf5"
    main(hdf5_filename, "train_history.tsv", "eval_history.tsv")
