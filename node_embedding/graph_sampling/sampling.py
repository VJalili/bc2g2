import h5py
import math
import models
import numpy as np
import os
import random

from sqlalchemy import or_
from sqlalchemy.orm import Session
from sqlalchemy.orm import joinedload
from sqlalchemy.sql.expression import func, select

# NOTE: All the nodes a graph should be connected to at least one node.


class Degree(models.B64Hashable):
    def __init__(self, in_degree: int, out_degree: int, self_loop: int):
        self.in_degree = in_degree
        self.out_degree = out_degree

        # some applications make decisions based on
        # degree excluding self-loops, hence for a
        # wider applicability, self-loops are
        # stored separately.
        self.self_loop = self_loop

    @property
    def degree(self):
        return self.in_degree + self.out_degree


class Graph(models.B64Hashable):
    def __init__(self, edges: set[models.Edge]):
        self.edges = edges
        self._degrees = {}
        self.update_degrees(self.edges)

    def update_degrees(self, edges: set[models.Edge]):
        for edge in edges:
            self.update_degree(edge)

    def update_degree(self, edge: models.Edge):
        if edge.source == edge.target:
            if edge.source not in self._degrees:
                self._degrees[edge.source] = Degree(0, 0, 1)
            else:
                self._degrees[edge.source].self_loop += 1

        if edge.source not in self._degrees:
            self._degrees[edge.source] = Degree(0, 1, 0)
        else:
            self._degrees[edge.source].out_degree += 1

        if edge.target not in self._degrees:
            self._degrees[edge.target] = Degree(1, 0, 0)
        else:
            self._degrees[edge.target].in_degree += 1

    def pop_edge(self):
        """
        Removes an edge from the graph and returns it,
        such that all the nodes are still connected to
        the graph with at least one edge. Returns
        None if such an edge cannot be found.
        """
        for edge in self.edges:
            if edge.source == edge.target:
                continue
            sd = self._degrees[edge.source]
            td = self._degrees[edge.target]

            if sd.in_degree + sd.out_degree > 1 and td.in_degree + td.out_degree > 1:
                self.edges.remove(edge)
                self.update_degrees(self.edges)
                return edge
        return None


class Sampler:
    def __init__(self):
        self.engine = models.get_engine()
        self.rnd_seed = 4
        # The seed needs to be in this range, so we can safely divide
        # it by 100 to get a seed between 0 and 1 to be compatible with
        # SQLAlchemy/PostgreSQL.
        if self.rnd_seed < 0 or self.rnd_seed > 99:
            raise ValueError(f"The given random seed {self.rnd_seed} is not in the 0<seed<100 range.")
        random.seed(self.rnd_seed)

    def _set_sql_random_seed(self, seed):
        """
        Do not set this seed once for all the random functions since
        some operations need different seed so they can generate
        different random numbers.

        Two caveats:
        a: this method is compatible with PostgreSQL, this needs to be changed if the backend engine is changed;
        b: alternative is using `func.setseed(SEED)`, but that did not work as expected in our tests.
        """
        # This is required for postgresql random function.
        if seed < 0 or seed > 1:
            raise ValueError(f"The given SQL seed {seed} is not in the 0<seed<1 range.")
        with Session(self.engine) as session:
            session.execute(f"select setseed({seed})")

    def _get_edges(self, node_id: int):
        with Session(self.engine) as session:
            return session.query(models.Edge)\
                .options(joinedload(models.Edge.source))\
                .options(joinedload(models.Edge.target))\
                .filter(or_(
                    models.Edge.source_id == node_id,
                    models.Edge.target_id == node_id)).all()

    def sample_nodes(self, count: int, seed=None):
        with Session(self.engine) as session:
            self._set_sql_random_seed(seed or self.rnd_seed / 100)
            return session.query(models.Node).order_by(func.random()).limit(count).all()

    def get_neighbors(self, node_id: int, hops: int):
        hops -= 1
        edges = self._get_edges(node_id)
        if len(edges) == 0:
            # TODO: is this the best approach?
            return

        if hops > 0:
            targets_ids = [e.target_id for e in edges]
            for tid in targets_ids:
                edges.extend(self.get_neighbors(tid, hops))
        return edges

    def get_neighbors_count(self, source_id, node_count, nodes_set=None, edges_set=None):
        """samples a graph with the given number of nodes."""
        edges = self._get_edges(source_id)
        if len(edges) == 0:
            return

        nodes_set = nodes_set or set()
        edges_set = edges_set or set()

        for edge in edges:
            # Ensures no edges are added that would result in having more noded than expected
            if edge.source not in nodes_set and edge.target not in nodes_set:
                if len(nodes_set) + 2 > node_count:
                    continue
            elif edge.source not in nodes_set or edge.target not in nodes_set:
                if len(nodes_set) + 1 > node_count:
                    continue

            if edge not in edges_set:
                nodes_set.add(edge.source)
                nodes_set.add(edge.target)
                edges_set.add(edge)

            # continue the loop even if the given number nodes are determined,
            # because that will help get other edges between the already found
            # nodes (e.g., if there are multiple edges between two nodes,
            # this helps find all those edges).

        if len(nodes_set) < node_count:
            for edge in edges:
                edges_set.update(self.get_neighbors_count(
                    edge.source, node_count - len(nodes_set), nodes_set, edges_set))
                if len(nodes_set) < node_count:
                    edges_set.update(self.get_neighbors_count(
                        edge.target, node_count - len(nodes_set), nodes_set, edges_set))
        else:
            return list(edges_set)

    @staticmethod
    def get_components(g: Graph):
        node_features, edges, node_row_lookup = [], [], {}

        def add_node(node):
            row = node_row_lookup.get(node.id_generated, None)
            if row is None:
                row = len(node_features)
                node_row_lookup[node.id_generated] = row
                node_features.append(node.get_features())
                # For some corner cases, the segment_sum in the
                # edge network may miss the last nodes in a graph
                # that have only incoming connections.
                # Two options: either add a self-loop for every node,
                # or zero-pad the output of the edge network.
                add_edge(row, row, [0] * models.Edge.features_count)
            return row

        def add_edge(source_row, target_row, features):
            edges.append([source_row, target_row, features])

        for edge in g.edges:
            add_edge(add_node(edge.source),
                     add_node(edge.target),
                     edge.get_features())

        # Sorting is required in model training for segment_sum.
        edges.sort(key=lambda x: (x[0], x[1]))
        return node_features, [x[2] for x in edges], [[x[0], x[1]] for x in edges]

    def get_random_edges(self, edge_count) -> set[models.Edge]:
        edges = set()
        max_retries = 3
        while len(edges) < edge_count and max_retries > 0:
            max_retries -= 1
            nodes = self.sample_nodes(int(math.ceil(edge_count / 2)), seed=(self.rnd_seed + 1) / 100)
            for node in nodes:
                node_edges = self._get_edges(node.id_generated)
                if len(node_edges) == 0:
                    continue
                edges.add(node_edges[0])

                if len(edges) >= edge_count:
                    break
        return edges

    def sample_for_edge_prediction(self, graph_count=2, nodes_per_graph=3):
        node_features, edge_features, pair_indices, labels = [], [], [], []
        determined_g = set()

        max_retries = 3
        while len(node_features) < graph_count and max_retries > 0:
            max_retries -= 1
            root_nodes = self.sample_nodes(graph_count)

            for root_node in root_nodes:
                edges = self.get_neighbors_count(root_node.id_generated, nodes_per_graph)
                if not edges:
                    continue

                g = Graph(set(edges))

                edge = g.pop_edge()
                if edge is None:
                    continue

                if g in determined_g:
                    continue
                determined_g.add(g)

                nf, ef, pi = self.get_components(g)
                node_features.append(nf)
                edge_features.append(ef)
                pair_indices.append(pi)
                labels.append(edge)

        if max_retries == 0:
            print(f"Warning: found less graphs than requested; "
                  f"found {int(len(node_features) / 2)} pairs, requested {graph_count}.")
        return node_features, edge_features, pair_indices, labels

    def sample(self, root_node=None, hops=1, include_random_edges=True, for_edge_prediction=False, existing_graphs=None):
        node_features, edge_features, pair_indices, labels = [], [], [], []
        existing_graphs = existing_graphs or set()
        root_node = root_node or self.sample_nodes(1)[0]

        max_retries = 3

        while max_retries > 0:
            print(f"\ttry {4 - max_retries}/3:")
            max_retries -= 1

            neighbors = self.get_neighbors(root_node.id_generated, hops)
            print("\t\tRetrieved neighbors.")
            if not neighbors:
                continue

            print(f"\t\tConstructing graph ... ", end="", flush=True)
            g1 = Graph(set(neighbors))
            print("Done.")

            if for_edge_prediction:
                # TODO: there is a corner case where you may
                #  remove the only not-to-self edge of a node,
                #  hence you may end-up with a graph that not
                #  all of its nodes are connected.
                edges = []
                extracted_edge = None
                for edge in g1.edges:
                    if edge.source.id_ != edge.target.id_ and extracted_edge is None:
                        extracted_edge = edge
                    else:
                        edges.append(edge)

                g1.edges = edges
                if extracted_edge is None:
                    continue

            if len(g1.edges) == 0:
                print("\n\nGraph does not have any edges.")
                continue
            g_hash = g1.__hash__()
            if g_hash in existing_graphs:
                print("\n\nGraph already exists.")
                continue

            if include_random_edges:
                print("\t\tGetting random edges ... ", end="", flush=True)
                g2 = Graph(self.get_random_edges(len(g1.edges)))
                print("Done.")
                if len(g2.edges) == 0:
                    print("\t\tRandom edges list empty.")
                    continue

            existing_graphs.add(g_hash)

            print("\t\tGraph to arrays ... ", end="", flush=True)
            nf1, ef1, pi1 = self.get_components(g1)
            node_features.append(nf1)
            edge_features.append(ef1)
            pair_indices.append(pi1)
            print("Done")
            if for_edge_prediction:
                labels.append(extracted_edge)
            else:
                labels.append(0)

            if include_random_edges:
                nf2, ef2, pi2 = self.get_components(g2)
                node_features.append(nf2)
                edge_features.append(ef2)
                pair_indices.append(pi2)
                labels.append(1)

            return node_features, edge_features, pair_indices, labels

        return None, None, None, None


# TODO: in some cases the positive and negative graphs do not
#  have the same number of nodes and/or edges. Is that a problem?

def main(graph_count=100, hops=2, filename="sampled_graphs.hdf5"):
    if os.path.isfile(filename):
        # TODO: inform the user file already exist, and take actions based on their choices.
        os.remove(filename)

    sampler = Sampler()

    print(f"Sampling {graph_count} nodes ... ", end="", flush=True)
    root_nodes = sampler.sample_nodes(graph_count)
    root_nodes_count = len(root_nodes)
    assert root_nodes_count == graph_count
    print("Done.")

    existing_graphs = set()
    root_node_counter, persisted_graphs_counter, missed = 0, -1, 0
    for root_node in root_nodes:
        root_node_counter += 1
        print(f"[{root_node_counter} / {root_nodes_count}] Processing root node {root_node.id_generated}:", flush=True)
        nodes, edges, pair_indices, labels = sampler.sample(
            root_node=root_node, hops=hops,
            include_random_edges=True,
            for_edge_prediction=False,
            existing_graphs=existing_graphs)

        if nodes is not None:
            print("\tPersisting ... ", end="", flush=True)
            persisted_graphs_counter += 1
            with h5py.File(filename, "a") as f:
                for group, i in [("graph", 0), ("random_edges", 1)]:
                    f.create_dataset(f"{persisted_graphs_counter}/{group}/node_features", data=nodes[i])
                    f.create_dataset(f"{persisted_graphs_counter}/{group}/edge_features", data=edges[i])
                    f.create_dataset(f"{persisted_graphs_counter}/{group}/pair_indices", data=pair_indices[i])
                    f.create_dataset(f"{persisted_graphs_counter}/{group}/labels", data=labels[i])
            print("Done.")
        else:
            print("\t\t!! Unable to create a graph with given parameters. !!")
            missed += 1

    if missed > 0:
        print(f"Warning! Requested {graph_count} graphs, but {graph_count - missed} were created.")
        print("Finished with warnings.")
    else:
        print("All process completed successfully.")


if __name__ == "__main__":
    main()
