using BC2G.Model;
using System.Collections.Concurrent;

namespace BC2G
{
    public class TxCache
    {
        public ConcurrentDictionary<string, Transaction> Utxo = new();

        public ConcurrentDictionary<string, Transaction> RecentTx = new();
    }
}
