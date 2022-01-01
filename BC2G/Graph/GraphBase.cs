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
                return new ReadOnlyCollection<Edge>(_edges.Keys.ToList());
            }
        }
        public ReadOnlyCollection<string> Nodes
        {
            get
            {
                return new ReadOnlyCollection<string>(_nodes.Keys.ToList());
            }
        }

        private readonly ConcurrentDictionary<Edge, byte> _edges = new();
        private readonly ConcurrentDictionary<string, byte> _nodes = new();

        public void AddEdge(Edge edge)
        {
            // The values are not used, disregard them.
            // C# does not have a ConcurrentHashSet, and 
            // the closest (and optimal one) is ConcurrentDictionary,
            // hence a ConcurrenctDictionary is used without 
            // needing its value.
            if (!_edges.TryAdd(edge, 0))
            {
                // testing only
            }
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
            // TODO: Can this method be optimized? 

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

            // Maybe a better way is to order the list 
            // of edges and compare similar to the 
            // nodes, but that may require more comparisons.
            var dict = new Dictionary<int, byte>(_edges.Count);
            foreach (var edge in _edges)
                dict.Add(edge.Key.GetHashCode(), 0);

            // why GetHashCode on a instance need a reference to self!?
            foreach (var edge in otherEdges)
                if (!dict.Remove(edge.GetHashCode()))
                    return false;

            if (dict.Count > 0)
                return false;

            return true;
        }
    }
}
