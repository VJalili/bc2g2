using System.Collections.Immutable;

namespace BC2G.Graph.Db.Neo4j;

public class BatchInfo
{
    public class TypeInfo
    {
        public int Count { set; get; }
        public string Filename { get; }

        public TypeInfo(string filename, int count)
        {
            Filename = filename;
            Count = count;
        }
    }

    public ImmutableDictionary<string, TypeInfo> TypeInfos
    {
        get
        {
            return _data.ToImmutableDictionary(
                x => x.Key.FullName ?? x.Key.Name,
                x => x.Value);
        }
    }

    private readonly string _dir;
    private readonly Dictionary<Type, TypeInfo> _data = new();

    public BatchInfo(string directory)
    {
        _dir = directory;
    }

    public void AddType(Type type, int count)
    {
        if (!_data.ContainsKey(type))
        {
            _data.Add(type, new TypeInfo(
                Path.Join(_dir, $"{type.GUID:N}_{DateTime.Now:yyyyMMddHHmmssffff}.csv"),
                0));
        }

        _data[type].Count += count;
    }

    public string GetFilename(Type type)
    {
        if (!_data.ContainsKey(type))
            return string.Empty;
        else
            return _data[type].Filename;
    }

    public int GetTotalCount()
    {
        return (from x in _data.Values select x.Count).Sum();
    }
}
