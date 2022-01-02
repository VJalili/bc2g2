using BC2G.Model;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace BC2G.Graph
{
    public abstract class GraphBase : IEquatable<GraphBase>
    {
        // Note that exposing as `readonly` does not prevent anyone from casting it 
        // to the original type and making changes. For a 'true' readonly type, 
        // you may return a copy of the collection instead.
        // TODO: test this ^^
        // TODO: improve on the immutability of the collections.

        public const string CoinbaseTxLabel = "Coinbase";

        public ReadOnlyCollection<Edge> Edges
        {
            get
            {
                return new ReadOnlyCollection<Edge>(_edges.Values.ToList());
            }
        }
        public ReadOnlyCollection<string> Nodes
        {
            get
            {
                return new ReadOnlyCollection<string>(_nodes.Keys.ToList());
            }
        }

        private readonly ConcurrentDictionary<int, Edge> _edges = new();
        private readonly ConcurrentDictionary<string, byte> _nodes = new();

        public void AddEdge(Edge edge)
        {
            /// Note that the hashkey is invariant to the edge value.
            /// If this is changed, the `Equals` method needs to be
            /// updated accordingly.
            _edges.AddOrUpdate(
                edge.GetHashCode(true), edge,
                (key, oldValue) => new Edge(
                    edge.Source,
                    edge.Target,
                    edge.Value + oldValue.Value,
                    edge.Type));

            _nodes.TryAdd(edge.Source, 0);
            _nodes.TryAdd(edge.Target, 0);
        }

        public void AddEdges(ICollection<Edge> edges)
        {
            foreach (var edge in edges)
                AddEdge(edge);
        }

        public bool Equals(GraphBase? other)
        {
            if (other == null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            var otherNodes = other.Nodes;
            if (_nodes.Count != otherNodes.Count)
                return false;

            var otherEdges = other.Edges;
            if (_edges.Count != otherEdges.Count)
                return false;

            var equal = Enumerable.SequenceEqual(
                Nodes.OrderBy(x => x),
                otherNodes.OrderBy(x => x));

            if (!equal)
                return false;

            var hashes = new HashSet<int>(_edges.Keys);
            foreach (var edge in otherEdges)
                /// Note that this hash method does not include
                /// edge value in the computation of hash key;
                /// this is in accordance with home with _edges.Keys
                /// are generated in the AddEdge method.
                if (!hashes.Remove(edge.GetHashCode(true)))
                    return false;

            if (hashes.Count > 0)
                return false;

            return true;
        }
    }
}
