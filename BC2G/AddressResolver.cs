using BC2G.Bookmarking;
using BC2G.Model;
using System.Collections.Concurrent;

namespace BC2G
{
    public class AddressResolver : PersistentDictionary<string, int>
    {
        private const char _delimiter = '\t';
        private readonly string _filename = string.Empty;

        private ConcurrentDictionary<string, int> _addresses = new();

        public AddressResolver(string filename) : base(filename)
        {

        }

        public override void Deserialize()
        {
            _addresses = new ConcurrentDictionary<string, int>();
            using var reader = new StreamReader(_filename);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var sLine = line.Split(_delimiter);
                _addresses.TryAdd(sLine[1], int.Parse(sLine[0]));
            }
        }

        public int Resolve(Output output)
        {
            var base64Encoding = output.ToBase64String();
            if (_addresses.TryGetValue(base64Encoding, out var id))
                return id;
            _addresses.TryAdd(base64Encoding, _addresses.Count);
            return _addresses[base64Encoding];
        }
    }
}
