import models
import numpy as np
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

    def _sample_nodes(self, count: int, seed=None):
        with Session(self.engine) as session:
            self._set_sql_random_seed(seed or self.rnd_seed / 100)
            return session.query(models.Node).order_by(func.random()).limit(count).all()

    def _get_node_features(self, node):
        row = self.nodes.loc[self.nodes[NODE_COL_NAME] == node]
        return row.loc[:, row.columns != NODE_COL_NAME].values[0].tolist()

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

    def get_random_edges(self, edge_count) -> list[models.Edge]:
        edges = []
        max_retries = 3
        while len(edges) < edge_count and max_retries > 0:
            max_retries -= 1
            nodes = self._sample_nodes(int(edge_count / 2), seed=(self.rnd_seed + 1) / 100)
            for node in nodes:
                node_edges = self._get_edges(node.id_generated)
                if len(node_edges) == 0:
                    continue
                edges.append(node_edges[0])

                if len(edges) >= edge_count:
                    break
        return edges

    def sample_for_edge_prediction(self, graph_count=2, nodes_per_graph=3):
        node_features, edge_features, pair_indices, labels = [], [], [], []
        determined_g = set()

        max_retries = 3
        while len(node_features) < graph_count and max_retries > 0:
            max_retries -= 1
            root_nodes = self._sample_nodes(graph_count)

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

    def sample(self, count=2, hops=1, include_random_edges=True, for_edge_prediction=False):
        node_features, edge_features, pair_indices, labels = [], [], [], []
        existing_g = set()

        max_retries = 3
        denominator = 2 if include_random_edges else 1

        while len(node_features) / denominator < count and max_retries > 0:
            print(f"Sampling graphs ... try {max_retries - 3}/3")
            max_retries -= 1
            root_nodes = self._sample_nodes(count)
            root_nodes_count = len(root_nodes)
            print(f"Sampled root nodes count: {root_nodes_count}")

            root_node_counter = 0
            for root_node in root_nodes:
                root_node_counter += 1
                print(f"processing root node with id {root_node.id_generated}: {root_node_counter}/{root_nodes_count}")
                neighbors = self.get_neighbors(root_node.id_generated, hops)
                print("retrieved neighbors")
                if not neighbors:
                    continue

                print(f"constructing graph ... ", end="", flush=True)
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
                    continue
                g_hash = g1.__hash__()
                if g_hash in existing_g:
                    continue

                if include_random_edges:
                    print("Getting random edges ... ", end="", flush=True)
                    g2 = Graph(self.get_random_edges(len(g1.edges)))
                    print("Done.")
                    if len(g2.edges) == 0:
                        continue

                existing_g.add(g_hash)

                print("Graph to arrays ... ", end="", flush=True)
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

        if max_retries == 0:
            print(f"Warning: found less graphs than requested; "
                  f"found {int(len(node_features) / 2)} pairs, requested {count}.")
        return node_features, edge_features, pair_indices, labels


def main():
    sampler = Sampler()
    nodes, edges, pair_indices, labels = sampler.sample(
        count=1000, hops=3, include_random_edges=True, for_edge_prediction=False)

    print("Serializing now ...")
    np.save("nodes_features", nodes)
    np.save("edges_features", edges)
    np.save("pair_indices", pair_indices)
    np.save("labels", labels)
    print("All process completed successfully.")


if __name__ == "__main__":
    main()
