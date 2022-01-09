using BC2G.Model;
using System.Collections.Concurrent;

namespace BC2G
{
    public class AddressResolver
    {
        private const char _delimiter = '\t';
        private readonly string _filename = string.Empty;

        private ConcurrentDictionary<string, string> _addresses = new();

        public AddressResolver(string filename)
        {
            _filename = filename;
            if (!File.Exists(_filename))
                File.Create(_filename);
            Deserialize();
        }


        public string Resolve(Output output)
        {
            _addresses.TryGetValue()
        }

        private void Deserialize()
        {
            _addresses = new ConcurrentDictionary<string, string>();
            using (var reader = new StreamReader(_filename))
            {
                string? line;
                while((line = reader.ReadLine()) != null)
                {
                    var sLine = line.Split(_delimiter);
                    _addresses.TryAdd(sLine[0], sLine[1]);
                }
            }
        }
    }
}
