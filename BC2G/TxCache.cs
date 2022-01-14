using BC2G.DTO;
using System.Collections.Concurrent;

namespace BC2G
{
    public class TxCache : IDisposable
    {
        public ConcurrentDictionary<string, OutputDTO> Utxo = new();

        public ConcurrentBag<string> RecentTx = new();

        private readonly string _outputDir;
        private bool _disposed = false;
        private const string _txidVoutDelimiter = "___";
        private const char _delimiter = '\t';

        public TxCache(string outputDir)
        {
            _outputDir = outputDir;
        }

        public bool TryGet(string txid, int outputIndex, out string address, out double value)
        {
            var res = Utxo.TryGetValue(ComposeId(txid, outputIndex), out OutputDTO output);
            if (res)
            {
                address = output.Address;
                value = output.Value;
            }
            else
            {
                address = string.Empty;
                value = 0;
            }
            return res;
        }

        public void Add(string txid, int outputIndex, string address, double value)
        {
            Utxo.TryAdd(
                ComposeId(txid, outputIndex),
                new OutputDTO(address, value));

            RecentTx.Add(
                $"{txid}{_delimiter}" +
                $"{outputIndex}{_delimiter}" +
                $"{address}{_delimiter}" +
                $"{value}");
        }

        private static string ComposeId(string txid, int outputIndex)
        {
            return $"{txid}{_txidVoutDelimiter}{outputIndex}";
        }

        // The IDisposable interface is implemented following .NET docs:
        // https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-6.0
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    /*
                    if (!string.IsNullOrEmpty(_filename))
                    {
                        var tmpMF = _filename + _tmpFilenamePostfix;
                        Serialize(tmpMF);
                        File.Move(tmpMF, _filename, true);
                    }*/
                }
            }

            _disposed = true;
        }
    }
}
