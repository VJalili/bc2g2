namespace BC2G.Graph.Db.Neo4j;

public interface IMapper<T>
{
    public string GetCsv(T entity);
    public string GetCsvHeader();
    public string GetQuery(string csvFilename);
}
