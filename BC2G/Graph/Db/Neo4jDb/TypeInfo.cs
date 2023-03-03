namespace BC2G.Graph.Db.Neo4jDb;

public class TypeInfo
{
    public int Order { get; }
    public int Count { set; get; }
    public string Filename { get; }

    [JsonConstructor]
    public TypeInfo(string filename, int count, int order)
    {
        Filename = filename;
        Count = count;
        Order = order;
    }
}
