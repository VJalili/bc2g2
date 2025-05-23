﻿namespace BC2G.Blockchains.Bitcoin.Graph;

public class ScriptNode : Node, IComparable<ScriptNode>, IEquatable<ScriptNode>
{
    public static GraphComponentType ComponentType
    {
        get { return GraphComponentType.BitcoinScriptNode; }
    }
    public override GraphComponentType GetGraphComponentType()
    {
        return GraphComponentType.BitcoinScriptNode;
    }

    // TODO: since there is a CoinbaseNode type, this default should change
    public string Address { get; } = BitcoinAgent.Coinbase;
    // TODO: since there is a CoinbaseNode type, this default should change
    public ScriptType ScriptType { get; } = ScriptType.Coinbase;

    public static new string Header
    {
        get
        {
            return string.Join(Delimiter, new string[]
            {
                Node.Header,
                "ScriptType"
            });
        }
    }

    public ScriptNode(
        string id, double? originalIndegree = null, double? originalOutdegree = null) :
        base(id, originalIndegree: originalIndegree, originalOutdegree: originalOutdegree)
    { }

    public ScriptNode(Utxo utxo) : base(utxo.Id)
    {
        Address = utxo.Address;
        ScriptType = utxo.ScriptType;
    }

    public ScriptNode(
        string id,
        string address,
        ScriptType scriptType,
        double? originalIndegree = null,
        double? originalOutdegree = null) :
        this(id, originalIndegree: originalIndegree, originalOutdegree: originalOutdegree)
    {
        Address = address;
        ScriptType = scriptType;
    }

    public ScriptNode(Neo4j.Driver.INode node, double? originalIndegree = null, double? originalOutdegree = null) :
        this(node.ElementId,
            (string)node.Properties[Props.ScriptAddress.Name],
            Enum.Parse<ScriptType>((string)node.Properties[Props.ScriptType.Name]),
            originalIndegree: originalIndegree, originalOutdegree: originalOutdegree)
    { }

    public override string GetUniqueLabel()
    {
        return Address;
    }

    public static ScriptNode GetCoinbaseNode()
    {
        return new ScriptNode(BitcoinAgent.Coinbase);
    }

    public static new string[] GetFeaturesName()
    {
        return [nameof(ScriptType), .. Node.GetFeaturesName()];
    }

    public override double[] GetFeatures()
    {
        return [(double)ScriptType, .. base.GetFeatures()];
    }

    public override int GetHashCode()
    {
        // Do not add ID here, because ID is generated
        // in a multi-threaded process, hence cannot
        // guarantee a node's ID is reproducible.
        return HashCode.Combine(Address, ScriptType);
    }

    public int CompareTo(ScriptNode? other)
    {
        if (other == null) return -1;
        var r = Address.CompareTo(other.Address);
        if (r != 0) return r;
        return ScriptType.CompareTo(other.ScriptType);
    }

    public bool Equals(ScriptNode? other)
    {
        if (other == null)
            return false;

        return
            Address == other.Address &&
            ScriptType == other.ScriptType;
    }

    public override string ToString()
    {
        return string.Join(
            Delimiter,
            new string[]
            {
                base.ToString(),
                ScriptType.ToString("d")
            });
    }
}
