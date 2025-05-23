﻿using BC2G.Utilities;

namespace BC2G.Graph.Model;

public class Edge<TSource, TTarget> : IEdge<TSource, TTarget>
    where TSource : notnull, INode
    where TTarget : notnull, INode
{
    public static GraphComponentType ComponentType { get { return GraphComponentType.Edge; } }
    public virtual GraphComponentType GetGraphComponentType() => ComponentType;

    public string Id { get; }
    public TSource Source { get; }
    public TTarget Target { get; }
    public long Value { get; }
    public EdgeType Type { get; }
    public uint Timestamp { get; }
    public long BlockHeight { get; }

    public static string Header
    {
        get
        {
            return string.Join(_delimiter, new string[]
            {
                "Source",
                "Target",
                "Value",
                "EdgeType",
                "TimeOffsetFromGenesisBlock",
                "BlockHeight"
            });
        }
    }

    private const string _delimiter = "\t";

    public Edge(
        TSource source, TTarget target,
        long value, EdgeType type,
        uint timestamp, long blockHeight)
    {
        Source = source;
        Target = target;
        Value = value;
        Type = type;
        Timestamp = timestamp;
        BlockHeight = blockHeight;

        Id = GetHashCode(true);
    }

    public Edge(
        TSource source, TTarget target,
        IRelationship relationship)
    {
        Source = source;
        Target = target;
        Id = relationship.ElementId;
        Value = Helpers.BTC2Satoshi((double)relationship.Properties[Props.EdgeValue.Name]);
        Type = Enum.Parse<EdgeType>(relationship.Type);
        BlockHeight = (long)relationship.Properties[Props.Height.Name];
    }

    public static string[] GetFeaturesName()
    {
        return [
            nameof(Value),
            nameof(Type),
            nameof(BlockHeight) ];
    }

    public virtual double[] GetFeatures()
    {
        return
        [
            Helpers.Satoshi2BTC(Value),
            (double)Type,
            BlockHeight
        ];
    }

    public string GetHashCode(bool ignoreValue)
    {
        if (ignoreValue)
            return HashCode.Combine(Source.Id, Target.Id, Type, Timestamp).ToString();
        else
            return GetHashCode().ToString();
    }

    public int GetHashCodeInt(bool ignoreValue)
    {
        if (ignoreValue)
            return HashCode.Combine(Source.Id, Target.Id, Type, Timestamp);
        else
            return GetHashCode();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Source.Id, Target.Id, Value, Type, Timestamp);
    }
}
