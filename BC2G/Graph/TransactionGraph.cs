using BC2G.Model;
using System.Collections.Concurrent;

namespace BC2G.Graph
{
    public class TransactionGraph : GraphBase
    {
        public TransactionGraph() : base()
        { }

        public double TotalInputValue { set; get; }
        public double TotalOutputValue { set; get; }

        public ConcurrentDictionary<string, double> Sources { set; get; } = new();
        public ConcurrentDictionary<string, double> Targets { set; get; } = new();

        public string AddSource(string source, double value)
        {
            TotalInputValue += value;
            return AddOrUpdate(Sources, source, value, ScriptType.Unknown);
        }

        public string AddTarget(string target, double value, ScriptType scriptType)
        {
            TotalOutputValue += value;
            return AddOrUpdate(Targets, target, value, scriptType);
        }

        private static string AddOrUpdate(
            ConcurrentDictionary<string, double> collection,
            string address,
            double value,
            ScriptType scriptType)
        {
            collection.AddOrUpdate(
                address, Utilities.Round(value),
                (_, oldValue) => Utilities.Round(oldValue + value));

            return address;
        }
    }
}
