import sys

import h5py
import logging
import math
import matplotlib.pyplot as plt
import model
import numpy as np
import os
import pandas as pd
import seaborn as sns
import warnings

import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.utils import Sequence

from sklearn.manifold import TSNE


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

    def __init__(self, filename: str,
                 indices: list[int],
                 batch_size=16,
                 shuffle=True,
                 to_fit=True,
                 groups=None):
        self.filename = filename
        self.indices = indices
        self.batch_size = batch_size
        self.shuffle = shuffle
        self.to_fit = to_fit
        self.groups = groups or ["graph", "random_edges"]

    def __len__(self):
        return math.ceil(len(self.indices) / self.batch_size)

    def __getitem__(self, idx):
        batch_indices = self.indices[idx * self.batch_size:(idx + 1) * self.batch_size]
        return self.get_graphs(batch_indices)

    def get_graphs(self, indices):
        node_features, edge_features, pair_indices, labels = [], [], [], []
        with h5py.File(self.filename, "r") as f:
            for idx in indices:
                for group_key in self.groups:
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
            y = tf.ragged.constant(labels, dtype=tf.float32)
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
    def __init__(self, data_dir,
                 graphs_for_classifier,
                 graphs_to_embed_filename,
                 graphs_for_train_edge_predictor_filename,
                 graphs_for_val_edge_predictor_filename,
                 graphs_for_eval_edge_predictor_filename,
                 output_prefix=""):
        self.data_dir = data_dir
        self.classifier_model = None
        self.embedder_model = None
        self.node_features_count = 1
        self.edge_features_count = 4
        self.output_prefix = output_prefix
        self.graphs_for_classifier = graphs_for_classifier
        self.graphs_to_embed_filename = graphs_to_embed_filename
        # TODO: We should surely use only one hdf5 for these three.
        self.g4ep_train_filename = graphs_for_train_edge_predictor_filename
        self.g4ep_val_filename = graphs_for_val_edge_predictor_filename
        self.g4ep_eval_filename = graphs_for_eval_edge_predictor_filename
        # TODO: should create a class for all the hyperparams and pass instances of the class around.
        self.embedder_epochs = 100
        self.embedder_learning_rate = 5e-4
        self.embedder_batch_size = 16

    def _get_generators(self, graphs_filename, groups=None):
        with h5py.File(graphs_filename, "r") as f:
            indices = list(f.keys())
        permuted_indices = np.random.permutation(indices)

        pi1, pi2 = int(len(permuted_indices) * 0.75), int(len(permuted_indices) * 0.95)
        train_index = permuted_indices[: pi1]
        valid_index = permuted_indices[pi1: pi2]
        test_index = permuted_indices[pi2:]

        return \
            GraphGenerator(graphs_filename, train_index, groups=groups, batch_size=self.embedder_batch_size), \
            GraphGenerator(graphs_filename, valid_index, groups=groups, batch_size=self.embedder_batch_size), \
            GraphGenerator(graphs_filename, test_index, groups=groups, batch_size=self.embedder_batch_size)

    def run_pipeline(self):
        # Train/get classifier.
        classifier_model_dir = os.path.join(self.data_dir, "classifier_model")
        if os.path.isdir(classifier_model_dir):
            # TODO: it currently fails to correctly save/load the model.
            # self.classifier_model = keras.models.load_model(model_dir)
            raise NotImplemented()
        else:
            self.classifier_model, model_input, embedder_output = self.train_classifier()
            # TODO: currently there are issues saving/loading this model.
            # self.classifier_model.save(classifier_model_dir)
        self.classifier_model.trainable = False

        # Train/get embedder & and embed nodes in given graphs.
        self.embedder_model = keras.Model(inputs=model_input, outputs=embedder_output)
        self.embed_graph()

        self.make_edge_predictions()

    def train_classifier(self):
        train_gen, val_gen, eval_gen = self._get_generators(self.graphs_for_classifier)

        bcgm, model_input, embedder_output = model.BlockChainGraphModel.get_model(
            node_features_count=self.node_features_count,
            edge_features_count=self.edge_features_count)

        bcgm.compile(
            loss=tf.keras.losses.BinaryCrossentropy(),
            optimizer=tf.keras.optimizers.Adam(learning_rate=self.embedder_learning_rate),
            metrics=[
                tf.keras.metrics.AUC(),
                tf.keras.metrics.Accuracy(),
                tf.keras.metrics.KLDivergence(name="kld"),
                tf.keras.metrics.MeanAbsoluteError(name="mae"),
                tf.keras.metrics.Precision(),
                tf.keras.metrics.Recall()])

        train_history = bcgm.fit(
            train_gen,
            validation_data=val_gen,
            epochs=self.embedder_epochs,
            verbose=1,
            class_weight={0: 2.0, 1: 0.5})  # hence the loss is a weighted average

        eval_history = bcgm.evaluate(eval_gen, return_dict=True)

        training_metrics_filename = os.path.join(
            self.data_dir, self.output_prefix + "training_metrics.tsv")

        self.serialize_history(
            train_history.history,
            training_metrics_filename)

        self.plot_history(
            training_metrics_filename,
            os.path.join(self.data_dir, self.output_prefix + "training_metrics.pdf"))

        self.serialize_history(
            eval_history,
            os.path.join(self.data_dir, self.output_prefix + "evaluation_metrics.tsv"))

        return bcgm, model_input, embedder_output

    def embed_graph(self):
        with h5py.File(self.graphs_to_embed_filename, "r") as f:
            indices = list(f.keys())
        generator = GraphGenerator(self.graphs_to_embed_filename, indices)
        x, y = generator.get_graphs(indices)
        embeddings = self.embedder_model.predict(generator)

        # tSNE plot the embeddings.
        trans = TSNE(n_components=2)
        emb_transformed = pd.DataFrame(trans.fit_transform(embeddings))  # TODO: , index=node_ids
        emb_transformed["label"] = y
        df = pd.DataFrame(data={
            "dim 1": emb_transformed[0],
            "dim 2": emb_transformed[1],
            "label": emb_transformed["label"].astype("category")})

        base_filename = os.path.join(self.data_dir, self.output_prefix + "node_embedding_tsne")
        df.to_csv(base_filename + ".tsv", sep="\t")
        sns.set_theme()
        sns.set_context("paper")
        ax = sns.scatterplot(data=df, x="dim 1", y="dim 2", hue="label")
        ax.set_title(f"{TSNE.__name__} visualization of node embeddings")
        ax.set_xlabel("Dimension 1")
        ax.set_ylabel("Dimension 2")
        plt.legend([], [], frameon=False)  # hide legend
        plt.savefig(base_filename + ".pdf")

    def make_edge_predictions(self):
        # TODO: this can be abstracted/simplified using the _get_generators method.
        def get_data(f_name):
            with h5py.File(f_name, "r") as f:
                indices = list(f.keys())
            generator = GraphGenerator(f_name, indices, groups=["graph"])
            _, y = generator.get_graphs(indices)
            embeddings = self.embedder_model.predict(generator)

            # It seems this conversion is essential as the model cannot train
            # when x and y are of different types (ragged tensor and np array here).
            y = y.to_list()
            embeddings = embeddings.tolist()

            return embeddings, y

        embeddings_train, y_train = get_data(self.g4ep_train_filename)
        embeddings_val, y_val = get_data(self.g4ep_val_filename)
        embeddings_eval, y_eval = get_data(self.g4ep_eval_filename)

        input_layer = layers.Input(shape=len(embeddings_train[0]), dtype="float32")
        x = layers.Dense(128, activation="relu")(input_layer)
        x = layers.Dense(32, activation="relu")(x)
        output_layer = layers.Dense(self.edge_features_count, activation="relu")(x)
        edge_model = tf.keras.Model(inputs=input_layer, outputs=output_layer)
        edge_model.compile(
            optimizer=keras.optimizers.Adam(learning_rate=5e-4),
            loss=keras.losses.BinaryCrossentropy())

        train_history = edge_model.fit(
            embeddings_train, y_train,
            validation_data=(embeddings_val, y_val),
            epochs=100, verbose=1)

        eval_history = edge_model.evaluate(
            embeddings_eval, y_eval, return_dict=True)

        self.serialize_history(
            train_history.history,
            os.path.join(self.data_dir, self.output_prefix + "edge_predictor_model_training.tsv"))

        self.serialize_history(
            eval_history,
            os.path.join(self.data_dir, self.output_prefix + "edge_predictor_model_evaluation.tsv"))

    @staticmethod
    def serialize_history(history, filename):
        df = pd.DataFrame.from_dict(history, orient="index").transpose()
        df.to_csv(filename, sep="\t")

    @staticmethod
    def plot_history(history_filename, filename):
        history_df = pd.read_csv(history_filename, sep="\t", index_col=0)
        sns.set_theme()
        sns.set_context("paper")
        ax = sns.lineplot(data=history_df, x=history_df.index, y="loss", label="Train Loss")
        ax = sns.lineplot(data=history_df, x=history_df.index, y="val_loss", label="Validation Loss")
        ax.legend()
        ax.set_ylabel("Loss")
        ax.figure.savefig(filename)


def main(data_dir,
         graphs_for_classifier,
         graphs_to_embed_filename,
         graphs_for_train_edge_predictor_filename,
         graphs_for_val_edge_predictor_filename,
         graphs_for_eval_edge_predictor_filename,
         output_prefix,
         embedder_epochs=100,
         embedder_learning_rate=5e-4,
         batch_size=16):

    encoder = GraphEncoder(
        data_dir=data_dir,
        graphs_for_classifier=graphs_for_classifier,
        graphs_to_embed_filename=graphs_to_embed_filename,
        graphs_for_train_edge_predictor_filename=graphs_for_train_edge_predictor_filename,
        graphs_for_val_edge_predictor_filename=graphs_for_val_edge_predictor_filename,
        graphs_for_eval_edge_predictor_filename=graphs_for_eval_edge_predictor_filename,
        output_prefix=output_prefix)
    encoder.embedder_epochs = embedder_epochs
    encoder.embedder_learning_rate = embedder_learning_rate
    encoder.embedder_batch_size = batch_size
    encoder.run_pipeline()


if __name__ == "__main__":
    if len(sys.argv) < 8:
        print("\nMissing args ... usage example (all arguments are required):")
        print("python embedder.py "
              "[data directory] "
              "[hdf5 containing graphs for classifier] "
              "[hdf5 containing graphs for embedding] "
              "[hdf5 containing graphs for training the edge predictor model] "
              "[hdf5 containing graphs for validating the edge predictor model] "
              "[hdf5 containing graphs for evaluating the edge predictor model] "
              "[output prefix]")
        exit()
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4], sys.argv[5], sys.argv[6], sys.argv[7])
