import base64
import pickle


# VERY IMPORTANT NOTE: NO NODE WITH NO EDGES SHOULD EXIST.


class B64Hashable:
    def encode(self):
        return base64.b64encode(pickle.dumps(self))

    def __hash__(self):
        return abs(hash(self.encode()))

    def __eq__(self, other):
        return self.__hash__() == other.__hash__()

    def __ne__(self, other):
        return not self.__eq__(other)

    def __getstate__(self):
        # We do not consider private members
        # for the state of an object, hence, we
        # do not pickle (and eventually hash) them.
        return {k: v for k, v in self.__dict__.items()
                if not k.startswith("_")}

    def __setstate__(self, state):
        self.__dict__.update(state)


class Node(B64Hashable):
    def __init__(self, id_: str, features: list[float]):
        self.id_ = id_
        self.features = features

    def __str__(self):
        return f"{self.id_}"

    __repr__ = __str__


class Edge(B64Hashable):
    def __init__(self, source: Node, target: Node, features: list[float]):
        self.source = source
        self.target = target
        self.features = features

    def __str__(self):
        return f"{self.source}->{self.target}"

    __repr__ = __str__


class Degree(B64Hashable):
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


class Graph(B64Hashable):
    def __init__(self, edges: set[Edge]):
        self.edges = edges
        self._degrees = {}
        self.update_degrees(self.edges)

    def update_degrees(self, edges: set[Edge]):
        for edge in edges:
            self.update_degree(edge)

    def update_degree(self, edge: Edge):
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
