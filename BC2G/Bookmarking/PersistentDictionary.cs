using System.Collections.Concurrent;
using System.Text;

namespace BC2G.Bookmarking
{
    public abstract class PersistentDictionary<TKey, TValue> :
        ConcurrentDictionary<TKey, TValue>, IDisposable
        where TKey : notnull, IEquatable<TKey>
    {
        private const string _delimiter = "\t";
        private const string _tmpFilenamePostfix = ".tmp";
        private readonly string _filename = string.Empty;
        private bool disposed = false;

        public PersistentDictionary(string filename)
        {
            _filename = filename;
            if (!File.Exists(_filename))
                File.Create(_filename);
            Deserialize();
        }

        public virtual void Serialize(string filename)
        {
            var builder = new StringBuilder();
            var e = GetEnumerator();
            while (e.MoveNext())
                builder.AppendLine(
                    $"{e.Current.Value}{_delimiter}{e.Current.Key}");

            File.WriteAllText(filename, builder.ToString());
        }

        public abstract void Deserialize();

        // The IDisposable interface is implemented following .NET docs:
        // https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-6.0
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (!string.IsNullOrEmpty(_filename))
                    {
                        var tmpMF = _filename + _tmpFilenamePostfix;
                        Serialize(tmpMF);
                        File.Move(tmpMF, _filename, true);
                    }
                }
            }

            disposed = true;
        }
    }
}
