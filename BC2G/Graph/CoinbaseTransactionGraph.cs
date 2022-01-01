using BC2G.Model;

namespace BC2G.Graph
{
    public class CoinbaseTransactionGraph : GraphBase
    {
        private readonly Dictionary<string, double> _targets;

        public CoinbaseTransactionGraph(int targetsCount)
        {
            _targets = new Dictionary<string, double>(targetsCount);
        }

        public void AddTarget(string target, double value)
        {
            if (!_targets.ContainsKey(target))
                _targets.Add(target, 0);

            _targets[target] = Utilities.Round(_targets[target] + value);
        }

        public void UpdateEdges()
        {
            foreach (var item in _targets)
                AddEdge(new Edge(
                    CoinbaseTxLabel,
                    item.Key,
                    item.Value,
                    EdgeType.Generation));
        }
    }
}
