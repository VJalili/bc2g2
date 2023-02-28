using BC2G.Exceptions;

namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class MapperFactory : IStrategyFactory
{
    public IMapperBase GetStrategyBase(string type)
    {
        try { return GetEdgeStrategy(type); }
        catch (MapperNotImplementedException) { }

        try { return GetGraphStrategy(type); }
        catch (MapperNotImplementedException) { }

        throw new MapperNotImplementedException(type);
    }

    public IEdgeMapper GetEdgeStrategy(string type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return type switch
        {
            string x when x == Utilities.TypeToString<S2SEdge>() => new S2SEdgeMapper(),
            string x when x == Utilities.TypeToString<T2TEdge>() => new T2TEdgeMapper(),
            string x when x == Utilities.TypeToString<C2SEdge>() => new C2SEdgeMapper(),
            string x when x == Utilities.TypeToString<C2TEdge>() => new C2TEdgeMapper(),
            _ => throw new MapperNotImplementedException(type)
        };
    }

    public IGraphMapper GetGraphStrategy(string type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        return type switch
        {
            string x when x == Utilities.TypeToString<BlockGraph>() => new BlockGraphMapper(),
            _ => throw new MapperNotImplementedException(type)
        };
    }
}
