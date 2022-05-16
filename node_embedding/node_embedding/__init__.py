
# import tensorflow as tf
# from tensorflow import keras
# from tensorflow.keras import layers
# import numpy as np
# import pandas as pd
# import matplotlib.pyplot as plt
# import warnings
# import logging
# import base64
# from pprint import pprint
# import random
# import pickle
#
# tf.get_logger().setLevel(logging.ERROR)
# warnings.filterwarnings("ignore")
#
# np.random.seed(42)
# tf.random.set_seed(42)
#
#
# NODE_COL_NAME = "node"
# SOURCE_COL_NAME = "source"
# TARGET_COL_NAME = "target"
# EDGE_FEATURE_COLUMNS = ["f1", "f2", "f3", "f4", "f5"]
#
# DELIMITER = "\t"
#
#
# # VERY IMPORTANT NOTE: NO NODE WITH NO EDGES SHOULD EXIST.
#
# class B64Hashable:
#     def encode(self):
#         return base64.b64encode(pickle.dumps(self))
#
#     def __hash__(self):
#         return abs(hash(self.encode()))
#
#     def __eq__(self, other):
#         return self.__hash__() == other.__hash__()
#
#     def __ne__(self, other):
#         return not self.__eq__(other)
#
#     def __getstate__(self):
#         # We do not consider private members
#         # for the state of an object, hence, we
#         # do not pickle (and eventually hash) them.
#         return {k: v for k, v in self.__dict__.items()
#                 if not k.startswith("_")}
#
#     def __setstate__(self, state):
#         self.__dict__.update(state)
#
#
# class Node(B64Hashable):
#     def __init__(self, id_: str, features: list[float]):
#         self.id_ = id_
#         self.features = features
#
#     def __str__(self):
#         return f"{self.id_}"
#
#     __repr__ = __str__
#
#
# class Edge(B64Hashable):
#     def __init__(self, source: Node, target: Node, features: list[float]):
#         self.source = source
#         self.target = target
#         self.features = features
#
#     def __str__(self):
#         return f"{self.source}->{self.target}"
#
#     __repr__ = __str__
#
#
# class Degree(B64Hashable):
#     def __init__(self, in_degree: int, out_degree: int, self_loop: int):
#         self.in_degree = in_degree
#         self.out_degree = out_degree
#
#         # some applications make decisions based on
#         # degree excluding self-loops, hence for a
#         # wider applicability, self-loops are
#         # stored separately.
#         self.self_loop = self_loop
#
#     @property
#     def degree(self):
#         return self.in_degree + self.out_degree
#
#
# class Graph(B64Hashable):
#     def __init__(self, edges: set[Edge]):
#         self.edges = edges
#         self._degrees = {}
#         self.update_degrees(self.edges)
#
#     def update_degrees(self, edges: set[Edge]):
#         for edge in edges:
#             self.update_degree(edge)
#
#     def update_degree(self, edge: Edge):
#         if edge.source == edge.target:
#             if edge.source not in self._degrees:
#                 self._degrees[edge.source] = Degree(0, 0, 1)
#             else:
#                 self._degrees[edge.source].self_loop += 1
#
#         if edge.source not in self._degrees:
#             self._degrees[edge.source] = Degree(0, 1, 0)
#         else:
#             self._degrees[edge.source].out_degree += 1
#
#         if edge.target not in self._degrees:
#             self._degrees[edge.target] = Degree(1, 0, 0)
#         else:
#             self._degrees[edge.target].in_degree += 1
#
#     def pop_edge(self):
#         """
#         Removes an edge from the graph and returns it,
#         such that all the nodes are still connected to
#         the graph with at least one edge. Returns
#         None if such an edge cannot be found.
#         """
#         for edge in self.edges:
#             if edge.source == edge.target:
#                 continue
#             sd = self._degrees[edge.source]
#             td = self._degrees[edge.target]
#
#             if sd.in_degree + sd.out_degree > 1 and td.in_degree + td.out_degree > 1:
#                 self.edges.remove(edge)
#                 self.update_degrees(self.edges)
#                 return edge
#         return None
#
#
# class Graph_old:
#     def __init__(self, nodes_filename, edges_filename):
#         self.nodes = pd.read_csv(nodes_filename, sep="\t")
#
#         print("\n----------------")
#
#         row_count, col_count = 0, 0
#         with open(edges_filename, "r") as f:
#             header = next(f)
#             col_count = len(header.split(DELIMITER))
#             for _ in f:
#                 row_count += 1
#
#         print(row_count)
#         print(col_count)
#         mem_map = np.memmap(edges_filename+".dat", dtype='float32', mode='w+', shape=(row_count, col_count))
#
#         row_counter = 0
#         with open(edges_filename, "r") as f:
#             next(f)
#             for l in f:
#                 cols = l.split(DELIMITER)
#                 mem_map[row_counter] = cols
#                 mem_map.flush()
#                 row_counter += 1
#
#         self.edges_2 = np.memmap(edges_filename+".dat", dtype='float32', mode='r', shape=(row_count, col_count))
#         print(f"\n\nshape:{self.edges_2.shape}\n")
#         print(self.edges_2)
#         print("\n")
#         mask = (self.edges_2[:, 0] == 2)
#         x = self.edges_2[mask, :]
#         print(x)
#         y = [e[1] for e in x]
#         print("\n\n")
#         print(y)
#         # exit()
#
#
#         self.edges = pd.read_csv(edges_filename, sep="\t")
#         self.rnd_seed = 1
#         random.seed(self.rnd_seed)
#
#     def _get_edges(self, node: str):
#         edges = self.edges.loc[
#             (self.edges[SOURCE_COL_NAME] == node) |
#             (self.edges[TARGET_COL_NAME] == node)]
#
#         print(f"\n\n\n-- node: {node}")
#         print("edges:")
#         print(edges)
#         print("\n\nedges 2:")
#         edges2 = self.edges_2
#
#         return [Edge(Node(edge[SOURCE_COL_NAME], self._get_node_features(edge[SOURCE_COL_NAME])),
#                      Node(edge[TARGET_COL_NAME], self._get_node_features(edge[TARGET_COL_NAME])),
#                      edge[EDGE_FEATURE_COLUMNS].to_list())
#                 for _, edge in edges.iterrows()]
#
#     def _get_node_features(self, node):
#         row = self.nodes.loc[self.nodes[NODE_COL_NAME] == node]
#         return row.loc[:, row.columns != NODE_COL_NAME].values[0].tolist()
#
#     def get_neighbors(self, source_id: str, hops: int):
#         hops -= 1
#         edges = self._get_edges(source_id)
#         if len(edges) == 0:
#             # TODO: is this the best approach?
#             return
#
#         if hops > 0:
#             for edge in edges:
#                 edges.extend(self.get_neighbors(edge.target.id_, hops))
#         else:
#             return edges
#
#     def get_neighbors_count(self, source_id, node_count, nodes_set=None, edges_set=None):
#         """samples a graph with the given number of nodes."""
#         edges = self._get_edges(source_id)
#         if len(edges) == 0:
#             return
#
#         nodes_set = nodes_set or set()
#         edges_set = edges_set or set()
#
#         for edge in edges:
#             # ensure you're not adding any edges that will result in having more nodes than the set limit.
#             if edge.source not in nodes_set and edge.target not in nodes_set:
#                 if len(nodes_set) + 2 > node_count:
#                     continue
#             elif edge.source not in nodes_set or edge.target not in nodes_set:
#                 if len(nodes_set) + 1 > node_count:
#                     continue
#
#             if edge not in edges_set:
#                 nodes_set.add(edge.source)
#                 nodes_set.add(edge.target)
#                 edges_set.add(edge)
#
#             # continue the loop even if the given number nodes are determined,
#             # because that will help get other edges between the already found
#             # nodes (e.g., if there are multiple edges between two nodes,
#             # this helps find all those edges).
#
#         if len(nodes_set) < node_count:
#             for edge in edges:
#                 edges_set.update(self.get_neighbors_count(
#                     edge.source, node_count - len(nodes_set), nodes_set, edges_set))
#                 if len(nodes_set) < node_count:
#                     edges_set.update(self.get_neighbors_count(
#                         edge.target, node_count - len(nodes_set), nodes_set, edges_set))
#         else:
#             return list(edges_set)
#
#     @staticmethod
#     def get_components(g: Graph):
#         node_features, edges, node_row_lookup = [], [], {}
#
#         def add_node(node):
#             row = node_row_lookup.get(node.id_, None)
#             if row is None:
#                 row = len(node_features)
#                 node_row_lookup[node.id_] = row
#                 node_features.append(node.features)
#                 # For some corner cases, the segment_sum in the
#                 # edge network may miss the last nodes in a graph
#                 # that have only incoming connections.
#                 # Two options: either add a self-loop for every node,
#                 # or zero-pad the output of the edge network.
#                 add_edge(row, row, [0] * len(EDGE_FEATURE_COLUMNS))
#             return row
#
#         def add_edge(source_row, target_row, features):
#             edges.append([source_row, target_row, features])
#
#         for edge in g.edges:
#             add_edge(add_node(edge.source),
#                      add_node(edge.target),
#                      edge.features)
#
#         # Sorting is required in model training for segment_sum.
#         edges.sort(key=lambda x: (x[0], x[1]))
#         return node_features, [x[2] for x in edges], [[x[0], x[1]] for x in edges]
#
#     def get_random_edges(self, edge_count) -> list[Edge]:
#         edges = []
#         max_retries = 3
#         while len(edges) < edge_count and max_retries > 0:
#             max_retries -= 1
#
#             nodes = self.nodes.sample(n=int(edge_count / 2), random_state=self.rnd_seed + 1)
#             for node in nodes[NODE_COL_NAME]:
#                 node_edges = self._get_edges(node)
#                 if len(node_edges) == 0:
#                     continue
#                 edges.append(node_edges[0])
#
#                 if len(edges) >= edge_count:
#                     break
#         return edges
#
#     def get_not_overlapping(self, node_count):
#         node_id_mapping = {}
#         node_features, edge_features, pair_indices = [], [], []
#         nodes = self.nodes.sample(n=node_count, random_state=self.rnd_seed + 1)
#
#         for node in nodes[NODE_COL_NAME]:
#             edges = self._get_edges(node)
#             if len(edges) == 0:
#                 continue
#
#             edge = edges.iloc[0]
#             neighbor = edge[TARGET_COL_NAME]
#
#             node_id_mapping[node] = len(node_id_mapping)
#             node_features.append(self._get_node_features(node))
#
#             if neighbor not in node_id_mapping:
#                 node_id_mapping[neighbor] = len(node_id_mapping)
#                 node_features.append(self._get_node_features(neighbor))
#
#             edge_features.append([v for k, v in edge.items() if k not in [SOURCE_COL_NAME, TARGET_COL_NAME]])
#             pair_indices.append([node_id_mapping[node], node_id_mapping[neighbor]])
#
#         return node_features, edge_features, pair_indices
#
#     def sample_for_edge_prediction(self, graph_count=2, nodes_per_graph=3):
#         node_features, edge_features, pair_indices, labels = [], [], [], []
#         determined_g = set()
#
#         max_retries = 3
#         while len(node_features) < graph_count and max_retries > 0:
#             max_retries -= 1
#             root_nodes = self.nodes.sample(n=graph_count, random_state=self.rnd_seed)
#
#             for root_node in root_nodes[NODE_COL_NAME]:
#                 edges = self.get_neighbors_count(root_node, nodes_per_graph)
#                 if not edges:
#                     continue
#
#                 g = Graph(set(edges))
#
#                 edge = g.pop_edge()
#                 if edge is None:
#                     continue
#
#                 if g in determined_g:
#                     continue
#                 determined_g.add(g)
#
#                 nf, ef, pi = self.get_components(g)
#                 node_features.append(nf)
#                 edge_features.append(ef)
#                 pair_indices.append(pi)
#                 labels.append(edge)
#
#         if max_retries == 0:
#             print(f"Warning: found less graphs than requested; "
#                   f"found {int(len(node_features) / 2)} pairs, requested {graph_count}.")
#         return node_features, edge_features, pair_indices, labels
#
#     def get_coocurring(self, existing_graphs, count=2, hops=1, include_random_edges=True, for_edge_prediction=False):
#         node_features, edge_features, pair_indices, labels = [], [], [], []
#         existing_g = set()
#
#         max_retries = 3
#         div_by = 2 if include_random_edges else 1
#
#         while len(node_features) / div_by < count and max_retries > 0:
#             max_retries -= 1
#             root_nodes = self.nodes.sample(n=count, random_state=self.rnd_seed)
#
#             for root_node in root_nodes[NODE_COL_NAME]:
#                 neighbors = self.get_neighbors(root_node, hops)
#                 if not neighbors:
#                     continue
#                 g1 = Graph(set(neighbors))
#
#                 if for_edge_prediction:
#                     # TODO: there is a corner case where you may
#                     #  remove the only not-to-self edge of a node,
#                     #  hence you may end-up with a graph that not
#                     #  all of its nodes are connected.
#                     edges = []
#                     extracted_edge = None
#                     for edge in g1.edges:
#                         if edge.source.id_ != edge.target.id_ and extracted_edge is None:
#                             extracted_edge = edge
#                         else:
#                             edges.append(edge)
#
#                     g1.edges = edges
#                     if extracted_edge is None:
#                         continue
#
#                 if len(g1.edges) == 0:
#                     continue
#                 g_hash = g1.__hash__()
#                 if g_hash in existing_g:
#                     continue
#
#                 if include_random_edges:
#                     g2 = Graph(self.get_random_edges(len(g1.edges)))
#                     if len(g2.edges) == 0:
#                         continue
#
#                 existing_g.add(g_hash)
#
#                 nf1, ef1, pi1 = self.get_components(g1)
#                 node_features.append(nf1)
#                 edge_features.append(ef1)
#                 pair_indices.append(pi1)
#                 if for_edge_prediction:
#                     labels.append(extracted_edge)
#                 else:
#                     labels.append(0)
#
#                 if include_random_edges:
#                     nf2, ef2, pi2 = self.get_components(g2)
#                     node_features.append(nf2)
#                     edge_features.append(ef2)
#                     pair_indices.append(pi2)
#                     labels.append(1)
#
#         if max_retries == 0:
#             print(f"Warning: found less graphs than requested; "
#                   f"found {int(len(node_features) / 2)} pairs, requested {count}.")
#         return node_features, edge_features, pair_indices, labels
#
#
# g = Graph_old("test_nodes.csv", "test_edges.csv")
# g_node_features, g_edge_features, g_pair_indices, labels = g.get_coocurring({}, include_random_edges=True, for_edge_prediction=False)
# print("\n\n>>>>>>>>>>>>>>>>>>\n--------------------node features:")
# pprint(g_node_features)
# print("\n--------------------edge features:")
# pprint(g_edge_features)
# print("\n--------------------pair indicies")
# pprint(g_pair_indices)
# print("\n--------------------labels:")
# pprint(labels)
#
# print("<<<<<<<<<<<<<<<<<<<<<<\n")
# all_g_node_features = []
# all_g_edge_features = []
# all_g_pair_indicies = []
# all_g_labels = []
#
# for i in range(100):
#     for j in range(len(g_node_features)):
#         all_g_node_features.append(np.array(g_node_features[j]))
#         all_g_edge_features.append(np.array(g_edge_features[j]))
#         all_g_pair_indicies.append(np.array(g_pair_indices[j]))
#         all_g_labels.append(labels[j])
#
# def get_dummy_graph_data():
#     return (
#         tf.ragged.constant(all_g_node_features, dtype=tf.float32),
#         tf.ragged.constant(all_g_edge_features, dtype=tf.float32),
#         tf.ragged.constant(all_g_pair_indicies, dtype=tf.int64),
#     )
#
#
# # Train set: 80 % of data
# x_train = get_dummy_graph_data()
# y_train = all_g_labels
#
#
# # Valid set: 19 % of data
# x_valid = get_dummy_graph_data()
# y_valid = all_g_labels
#
#
# # Test set: 1 % of data
# x_test = get_dummy_graph_data()
# y_test = all_g_labels
#
#
# def prepare_batch(x_batch, y_batch):
#     """
#     Merges (sub)graphs of batch into a single global (disconnected) graph
#     """
#
#     atom_features, bond_features, pair_indices = x_batch
#
#     # Obtain number of atoms and bonds for each graph (molecule)
#     num_atoms = atom_features.row_lengths()
#     num_bonds = bond_features.row_lengths()
#
#     # Obtain partition indices. atom_partition_indices will be used to
#     # gather (sub)graphs from global graph in model later on
#     molecule_indices = tf.range(len(num_atoms))
#     atom_partition_indices = tf.repeat(molecule_indices, num_atoms)
#     bond_partition_indices = tf.repeat(molecule_indices[:-1], num_bonds[1:])
#
#     # Merge (sub)graphs into a global (disconnected) graph. Adding 'increment' to
#     # 'pair_indices' (and merging ragged tensors) actualizes the global graph
#     increment = tf.cumsum(num_atoms[:-1])
#     increment = tf.pad(tf.gather(increment, bond_partition_indices), [(num_bonds[0], 0)])
#     pair_indices = pair_indices.merge_dims(outer_axis=0, inner_axis=1).to_tensor()
#     pair_indices = pair_indices + increment[:, tf.newaxis]
#     atom_features = atom_features.merge_dims(outer_axis=0, inner_axis=1).to_tensor()
#     bond_features = bond_features.merge_dims(outer_axis=0, inner_axis=1).to_tensor()
#
#     return (atom_features, bond_features, pair_indices, atom_partition_indices), y_batch
#
#
# def MPNNDataset(X, y, batch_size=32, shuffle=False):
#     dataset = tf.data.Dataset.from_tensor_slices((X, (y)))
#     if shuffle:
#         dataset = dataset.shuffle(1024)
#     return dataset.batch(batch_size).map(prepare_batch, -1)
#
#
# """
# ## Model
#
# The MPNN model can take on various shapes and forms. In this tutorial, we will implement an
# MPNN based on the original paper
# [Neural Message Passing for Quantum Chemistry](https://arxiv.org/abs/1704.01212) and
# [DeepChem's MPNNModel](https://deepchem.readthedocs.io/en/latest/api_reference/models.html#mpnnmodel).
# The MPNN of this tutorial consists of three stages: message passing, readout and
# classification.
#
#
# ### Message passing
#
# The message passing step itself consists of two parts:
#
# 1. The *edge network*, which passes messages from 1-hop neighbors `w^{t}_{i}` of `v^{t}`
# to `v^{t}`, based on the edge features between them (`e_{v^{t}w^{t}_{i}}`, where `t =
# 0`), resulting in an updated node state `v^{t+1}`. `_{i}` denotes the `i:th` neighbor of
# `v^{t}` and `^{t}` the `t:th` state of `v` or `w`. An important feature of the edge
# network (in contrast to e.g. the relational graph convolutional network) is that it
# allows for non-discrete edge features. However, in this tutorial, only discrete edge
# features will be used.
#
#
# 2. The *gated recurrent unit* (GRU), which takes as input the most recent node state
# (e.g., `v^{t+1}`) and updates it based on previous node state(s) (e.g., `v^{t}`). In
# other words, the most recent node states serves as the input to the GRU, while the previous
# node state(s) are incorporated within the memory state of the GRU.
#
# Importantly, step (1) and (2) are repeated for `k steps`, and where at each step `1...k`,
# the radius (or # hops) of aggregated information from the source node `v` increases by 1.
# """
#
#
# class EdgeNetwork(layers.Layer):
#     def __init__(self, **kwargs):
#         super().__init__(**kwargs)
#
#     def build(self, input_shape):
#         self.atom_dim = input_shape[0][-1]
#         self.bond_dim = input_shape[1][-1]
#         self.kernel = self.add_weight(
#             shape=(self.bond_dim, self.atom_dim * self.atom_dim),
#             trainable=True,
#             initializer="glorot_uniform",
#         )
#         self.bias = self.add_weight(
#             shape=(self.atom_dim * self.atom_dim), trainable=True, initializer="zeros",
#         )
#         self.built = True
#
#     def call(self, inputs):
#         atom_features, bond_features, pair_indices = inputs
#
#         # Apply linear transformation to bond features
#         bond_features = tf.matmul(bond_features, self.kernel) + self.bias
#
#         # Reshape for neighborhood aggregation later
#         bond_features = tf.reshape(bond_features, (-1, self.atom_dim, self.atom_dim))
#
#         # Obtain atom features of neighbors
#         atom_features_neighbors = tf.gather(atom_features, pair_indices[:, 1])
#         atom_features_neighbors = tf.expand_dims(atom_features_neighbors, axis=-1)
#
#         # Apply neighborhood aggregation
#         transformed_features = tf.matmul(bond_features, atom_features_neighbors)
#
#         transformed_features = tf.squeeze(transformed_features, axis=-1)
#
#         aggregated_features = tf.math.segment_sum(transformed_features, pair_indices[:, 0])
#         return aggregated_features
#
#
# class MessagePassing(layers.Layer):
#     def __init__(self, units, steps=4, **kwargs):
#         super().__init__(**kwargs)
#         self.units = units
#         self.steps = steps
#
#     def build(self, input_shape):
#         self.atom_dim = input_shape[0][-1]
#         self.message_step = EdgeNetwork()
#         self.pad_length = max(0, self.units - self.atom_dim)
#         self.update_step = layers.GRUCell(self.atom_dim + self.pad_length)
#         self.built = True
#
#     def call(self, inputs):
#         atom_features, bond_features, pair_indices = inputs
#
#         # Pad atom features if number of desired units exceeds atom_features dim
#         atom_features_updated = tf.pad(atom_features, [(0, 0), (0, self.pad_length)])
#
#         # Perform a number of steps of message passing
#         for i in range(self.steps):
#             # Aggregate atom_features from neighbors
#             atom_features_aggregated = self.message_step(
#                 [atom_features_updated, bond_features, pair_indices]
#             )
#
#             # Update aggregated atom_features via a step of GRU
#             atom_features_updated, _ = self.update_step(atom_features_aggregated, atom_features_updated)
#
#         return atom_features_updated
#
#
# """
# ### Readout
#
# When the message passing procedure ends, the k-step-aggregated node states are to be partitioned
# into subgraphs (correspoding to each molecule in the batch) and subsequently
# reduced to graph-level embeddings. In the
# [original paper](https://arxiv.org/abs/1704.01212), a
# [set-to-set layer](https://arxiv.org/abs/1511.06391) was used for this purpose.
# In this tutorial however, a transformer encoder will be used. Specifically:
#
# * the k-step-aggregated node states will be partitioned into the subgraphs
# (corresponding to each molecule in the batch);
# * each subgraph will then be padded to match the subgraph with the greatest number of nodes, followed
# by a `tf.stack(...)`;
# * the (stacked) padded tensor, encoding subgraphs (each subgraph containing sets of node states), are
# masked to make sure the paddings don't interfere with training;
# * finally, the padded tensor is passed to the transformer followed by an average pooling.
# """
#
#
# class PartitionPadding(layers.Layer):
#     def __init__(self, batch_size, **kwargs):
#         super().__init__(**kwargs)
#         self.batch_size = batch_size
#
#     def call(self, inputs):
#         atom_features, atom_partition_indices = inputs
#
#         # Obtain subgraphs
#         atom_features = tf.dynamic_partition(atom_features, atom_partition_indices, self.batch_size)
#
#         # Pad and stack subgraphs
#         num_atoms = [tf.shape(f)[0] for f in atom_features]
#         max_num_atoms = tf.reduce_max(num_atoms)
#         atom_features_padded = tf.stack(
#             [
#                 tf.pad(f, [(0, max_num_atoms - n), (0, 0)])
#                 for f, n in zip(atom_features, num_atoms)
#             ],
#             axis=0,
#         )
#
#         # Remove empty subgraphs (usually for last batch)
#         nonempty_examples = tf.where(tf.reduce_sum(atom_features_padded, (1, 2)) != 0)
#         nonempty_examples = tf.squeeze(nonempty_examples, axis=-1)
#
#         return tf.gather(atom_features_padded, nonempty_examples, axis=0)
#
#
# class TransformerEncoder(layers.Layer):
#     def __init__(self, num_heads=8, embed_dim=64, dense_dim=512, **kwargs):
#         super().__init__(**kwargs)
#
#         self.attention = layers.MultiHeadAttention(num_heads, embed_dim)
#         self.dense_proj = keras.Sequential(
#             [layers.Dense(dense_dim, activation="relu"), layers.Dense(embed_dim),]
#         )
#         self.layernorm_1 = layers.LayerNormalization()
#         self.layernorm_2 = layers.LayerNormalization()
#         self.supports_masking = True
#
#     def call(self, inputs, mask=None):
#         attention_mask = mask[:, tf.newaxis, :] if mask is not None else None
#         attention_output = self.attention(inputs, inputs, attention_mask=attention_mask)
#         proj_input = self.layernorm_1(inputs + attention_output)
#         return self.layernorm_2(proj_input + self.dense_proj(proj_input))
#
#
# """
# ### Message Passing Neural Network (MPNN)
#
# It is now time to complete the MPNN model. In addition to the message passing
# and readout, a two-layer classification network will be implemented to make
# predictions of BBBP.
# """
#
#
# def MPNNModel(
#         atom_dim,
#         bond_dim,
#         batch_size=32,
#         message_units=64,
#         message_steps=4,
#         num_attention_heads=8,
#         dense_units=512,
# ):
#     atom_features = layers.Input((atom_dim), dtype="float32", name="atom_features")
#     bond_features = layers.Input((bond_dim), dtype="float32", name="bond_features")
#     pair_indices = layers.Input((2), dtype="int32", name="pair_indices")
#     atom_partition_indices = layers.Input(
#         (), dtype="int32", name="atom_partition_indices"
#     )
#
#     x = MessagePassing(message_units, message_steps)(
#         [atom_features, bond_features, pair_indices]
#     )
#
#     x = PartitionPadding(batch_size)([x, atom_partition_indices])
#
#     x = layers.Masking()(x)
#
#     x = TransformerEncoder(num_attention_heads, message_units, dense_units)(x)
#
#     x = layers.GlobalAveragePooling1D()(x)
#     before_last = layers.Dense(dense_units, activation="relu")(x)
#     x = layers.Dense(1, activation="sigmoid")(before_last)
#
#     _inputs = [atom_features, bond_features, pair_indices, atom_partition_indices]
#     _embeder_output = before_last
#     model = keras.Model(
#         inputs=[atom_features, bond_features, pair_indices, atom_partition_indices],
#         outputs=[x],
#     )
#     return model, _inputs, _embeder_output
#
#
# mpnn, __inputs, __embedder_output = MPNNModel(
#     atom_dim=x_train[0][0][0].shape[0], bond_dim=x_train[1][0][0].shape[0],
# )
#
# mpnn.compile(
#     loss=keras.losses.BinaryCrossentropy(),
#     optimizer=keras.optimizers.Adam(learning_rate=5e-4),
#     metrics=[keras.metrics.AUC(name="AUC")],
# )
#
# # keras.utils.plot_model(mpnn, show_dtype=True, show_shapes=True)
#
# """
# ### Training
# """
#
# train_dataset = MPNNDataset(x_train, y_train)
# valid_dataset = MPNNDataset(x_valid, y_valid)
# test_dataset = MPNNDataset(x_test, y_test)
#
# history = mpnn.fit(
#     train_dataset,
#     validation_data=valid_dataset,
#     epochs=10,#40,
#     verbose=2,
#     class_weight={0: 2.0, 1: 0.5},
# )
#
# plt.figure(figsize=(10, 6))
# plt.plot(history.history["AUC"], label="train AUC")
# plt.plot(history.history["val_AUC"], label="valid AUC")
# plt.xlabel("Epochs", fontsize=16)
# plt.ylabel("AUC", fontsize=16)
# plt.legend(fontsize=16)
#
# """
# ### Predicting
# """
# #
# # molecules = [molecule_from_smiles(df.smiles.values[index]) for index in test_index]
# # y_true = [df.p_np.values[index] for index in test_index]
# # y_pred = tf.squeeze(mpnn.predict(test_dataset), axis=1)
# #
# # legends = [f"y_true/y_pred = {y_true[i]}/{y_pred[i]:.2f}" for i in range(len(y_true))]
# # MolsToGridImage(molecules, molsPerRow=4, legends=legends)
#
#
#
# print(f"now freezing.")
# mpnn.trainable = False
# embedding_model = keras.Model(inputs=__inputs, outputs=__embedder_output)
# # keras.utils.plot_model(embedding_model, to_file="embedding_model.png", show_dtype=True, show_shapes=True)
#
#
# from sklearn.manifold import TSNE
# import pandas as pd
# import numpy as np
# import matplotlib.pyplot as plt
#
# # TODO: check why I need to pass train_dataset (ie., containing both data AND
# #  labels instead of just the graph, pass labels does not make sense).
# test_y_train = [0] * len(y_train)
# test_embedding_dataset = MPNNDataset(x_train, test_y_train)
# embeddings = embedding_model.predict(train_dataset)
# print("\nembeddings:\n")
# print(embeddings)
# print("\n-------------")
# trans = TSNE(n_components=2)
# emb_transformed = pd.DataFrame(trans.fit_transform(embeddings))  # TODO: , index=node_ids
# emb_transformed["label"] = y_train
#
# alpha = 0.7
#
# fig, ax = plt.subplots(figsize=(7, 7))
# ax.scatter(
#     emb_transformed[0],
#     emb_transformed[1],
#     c=emb_transformed["label"].astype("category"),
#     cmap="jet",
#     alpha=alpha,
# )
# ax.set(aspect="equal", xlabel="$X_1$", ylabel="$X_2$")
# plt.title(
#     "{} visualization of GraphSAGE embeddings for cora dataset".format(TSNE.__name__)
# )
# # plt.show()
#
#
# # This is part for using embeddings for link prediction
# print("\n\n---------------------- for edge prediction")
# g = Graph_old("test_nodes.csv", "test_edges.csv")
# g_node_features__2, g_edge_features__2, g_pair_indices__2, labels__2 = g.sample_for_edge_prediction(2, 2)
# if len(g_node_features__2) == 0:
#     print("Warning: No graphs with the given criteria are found; exiting.")
#     exit()
#
#
# print("-----------------\nnode features:")
# pprint(g_node_features__2)
# print("edge features")
# pprint(g_edge_features__2)
# print("pair indices")
# pprint(g_pair_indices__2)
# print("----------------\n")
#
#
# all_g_node_features__2 = []
# all_g_edge_features__2 = []
# all_g_pair_indicies__2 = []
# all_g_labels__2 = []
#
# for i in range(3):
#     for j in range(len(g_node_features__2)):
#         all_g_node_features__2.append(np.array(g_node_features__2[j]))
#         all_g_edge_features__2.append(np.array(g_edge_features__2[j]))
#         all_g_pair_indicies__2.append(np.array(g_pair_indices__2[j]))
#         all_g_labels__2.append(labels__2[j].features)
#
# print("\nall g-node features 2:")
# pprint(all_g_node_features__2)
#
# print("\nall g-edge features 2:")
# pprint(all_g_edge_features__2)
#
# print("\nall g-pair indicies 2:")
# pprint(all_g_pair_indicies__2)
#
# print("\nall labels:")
# pprint(all_g_labels__2)
#
# def get_dummy_graph_data__2():
#     return (
#         tf.ragged.constant(all_g_node_features__2, dtype=tf.float32),
#         tf.ragged.constant(all_g_edge_features__2, dtype=tf.float32),
#         tf.ragged.constant(all_g_pair_indicies__2, dtype=tf.int64),
#     )
#
# x_train__2 = get_dummy_graph_data__2()
# y_train__2 = all_g_labels__2
#
# print("x-train")
# pprint(x_train__2)
#
# print("\ny-train")
# pprint(y_train__2)
#
# train_dataset__2 = MPNNDataset(x_train__2, y_train__2)
# embeddings = embedding_model.predict(train_dataset__2)
#
#
# in_size = embeddings.shape[1]
# out_size = len(all_g_labels__2[0])
#
# input_layer = layers.Input(shape=(in_size), dtype="float32")
# x = layers.Dense(10, activation="relu")(input_layer)
# output_layer = layers.Dense(out_size, activation="relu")(x)
# edge_model = tf.keras.Model(inputs=input_layer, outputs=output_layer)
# edge_model.compile(optimizer=keras.optimizers.Adam(learning_rate=5e-4), loss=keras.losses.BinaryCrossentropy())
# # keras.utils.plot_model(edge_model, show_dtype=True, show_shapes=True)
# edge_model.fit(embeddings.tolist(), y_train__2, epochs=10, verbose=2)
#
#
# # TODO: check how this can be used?
# # tf.squeeze(mpnn.predict(test_dataset), axis=1)
