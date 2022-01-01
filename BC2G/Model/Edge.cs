using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace BC2G.Model
{
    public class Edge : IEqualityComparer<Edge> //IEqualityComparer
    {
        public string Source { get; private set; }
        public string Target { get; private set; }
        public double Value { get; private set; }
        public EdgeType Type { get; private set; }

        public Edge(
            string source, string target,
            double value, EdgeType type)
        {
            Source = source;
            Target = target;
            Value = value;
            Type = type;
        }

        public bool Equals(Edge? x, Edge? y)
        {
            if (x == null && y != null)
                return false;
            if (x != null && y == null)
                return false;
            if (x == null && y == null)
                return true; // is this condition possible?

            return x.GetHashCode() == y.GetHashCode();
        }


        public int GetHashCode([DisallowNull] Edge obj)
        {
            return obj == null ? 0 : obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Source, Target, Value, Type);
        }
    }
}
