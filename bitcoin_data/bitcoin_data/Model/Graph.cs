using System.Collections.ObjectModel;

namespace bitcoin_data.Model
{
    internal class Graph
    {
        // To be consistent with Bitcoin client. 
        public const int FractionalDigitsCount = 8;

        public ReadOnlyCollection<Edge> Edges { get { return _edges.AsReadOnly(); } }

        private readonly HashSet<string> _nodes = new();
        private readonly List<Edge> _edges = new();

        private readonly Dictionary<string, double> _sources;
        private readonly Dictionary<string, double> _targets;

        public Graph(int sourceSize, int tagetSize)
        {
            _sources = new Dictionary<string, double>(sourceSize);
            _targets = new Dictionary<string, double>(tagetSize);
        }

        public void AddSource(string source, double value)
        {
            _sources.Add(source, value);
        }

        public void AddTarget(string taget, double value)
        {
            _targets.Add(taget, value);
        }

        public void UpdateEdges()
        {
            if (_sources.Count == 1 && _sources.ContainsKey("Coinbase"))
            {
                _nodes.Add("Coinbase");
                foreach (var item in _targets)
                    _edges.Add(new Edge("Coinbase", item.Key, item.Value, EdgeType.Generation));

                return;
            }

            double totalInputValue = 0, totalOutputValue = 0;
            foreach (var s in _sources)
            {
                totalInputValue += s.Value;
            }
            foreach (var s in _targets)
            {
                totalOutputValue += s.Value;
            }
            totalInputValue = Round(totalInputValue);
            totalOutputValue = Round(totalOutputValue);

            double fee = Round(totalInputValue - totalOutputValue);

            var changes = _targets.Where(x => _sources.ContainsKey(x.Key)).ToList();
            switch (changes.Count)
            {
                case 0: break;
                case 1:
                    _sources[changes[0].Key] = Round(_sources[changes[0].Key] - changes[0].Value);
                    _targets.Remove(changes[0].Key);
                    totalInputValue = Round(totalInputValue - changes[0].Value);
                    break;
                default:
                    // This is not expected to happen.
                    throw new NotImplementedException();

            }

            foreach (var s in _sources)
                _sources[s.Key] = Round(_sources[s.Key] - fee);

            // FIXME
            string minerAddress = string.Empty;

            foreach (var s in _sources)
            {
                foreach (var t in _targets)
                    _edges.Add(new Edge(
                        s.Key, t.Key,
                        Round(t.Value * (Round(s.Value / Round(totalInputValue - fee)))),
                        EdgeType.Transfer));
                _edges.Add(new Edge(s.Key, minerAddress, fee, EdgeType.Fee));
            }
        }

        private double Round(double input)
        {
            // Regarding the motivation behind this, read the following:
            // https://stackoverflow.com/q/588004/947889
            return Math.Round(input, digits: FractionalDigitsCount);
        }
    }
}
