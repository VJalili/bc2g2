using System.Collections.Immutable;

namespace BC2G.Graph.Db.Neo4jDb;

public class Batch
{
    public string Name { get; }
    public string DefaultDirectory { get; }

    public ImmutableSortedDictionary<string, TypeInfo> TypesInfo
    {
        get
        {
            return
                (from x in _typesInfo orderby x.Value.Order select x)
                .ToImmutableSortedDictionary();
        }
    }
    private readonly Dictionary<string, TypeInfo> _typesInfo;


    [JsonConstructor]
    public Batch(
        string name,
        string defaultDirectory,
        ImmutableSortedDictionary<string, TypeInfo> typesInfo)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        _typesInfo = new Dictionary<string, TypeInfo>(typesInfo);
    }

    public Batch(string name, string defaultDirectory, List<(string name, int order)> types)
    {
        Name = name;
        DefaultDirectory = defaultDirectory;
        var timestamp = Utilities.GetTimestamp();

        _typesInfo = new();
        foreach (var type in types)
            _typesInfo.Add(type.name, new TypeInfo(
                CreateFilename(type.name, timestamp, DefaultDirectory), 0, type.order));
    }

    public void AddOrUpdate(string type, int count)
    {
        AddOrUpdate(type, count, DefaultDirectory);
    }

    public void AddOrUpdate(string type, int count, string directory)
    {
        EnsureType(type, directory);
        _typesInfo[type].Count += count;
    }

    public string GetFilename(string type)
    {
        EnsureType(type, DefaultDirectory);
        return _typesInfo[type].Filename;
    }

    public int GetTotalCount()
    {
        return (from x in _typesInfo.Values select x.Count).Sum();
    }

    public TypeInfo GetTypeInfo(string type)
    {
        return _typesInfo[type];
    }

    private void EnsureType(string type, string directory)
    {
        if (!_typesInfo.ContainsKey(type))
        {
            _typesInfo.Add(type, new TypeInfo(
                CreateFilename(type, Utilities.GetTimestamp(), directory), 0));
        }
    }

    private static string CreateFilename(string type, string timestamp, string directory)
    {
        return Path.Join(directory, $"{timestamp}_{type.Replace('.', '_')}.csv");
    }
}
