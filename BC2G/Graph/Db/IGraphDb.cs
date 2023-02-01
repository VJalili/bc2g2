namespace BC2G.Graph.Db;

public interface IGraphDb<T> where T : GraphBase
{
    public void Import(T graph);
}
