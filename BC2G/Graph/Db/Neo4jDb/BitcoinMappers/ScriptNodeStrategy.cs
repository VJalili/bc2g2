namespace BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

public class ScriptNodeStrategy : NodeStrategyBase
{
    public const string labels = "Script";

    private readonly Property[] _properties = new Property[]
    {
        Props.ScriptAddress,
        Props.ScriptType
    };

    public override string GetCsvHeader()
    {
        return string.Join(
            csvDelimiter,
            from x in _properties select x.CsvHeader);
    }

    public override string GetCsv(Model.INode node)
    {
        return GetCsv((ScriptNode)node);
    }

    public static string GetCsv(ScriptNode node)
    {
        return string.Join(
            csvDelimiter,
            node.Address, node.ScriptType.ToString());
    }

    public override string GetQuery(string filename)
    {
        // The following is an example of the generated query.
        //
        // LOAD CSV WITH HEADERS FROM 'file:///filename.csv' AS line
        // FIELDTERMINATOR '	'
        // MERGE (node:Script {Address:line.Address})
        // SET
        //  node.ScriptType=line.ScriptType

        string l = Property.lineVarName, node = "node";

        var builder = new StringBuilder();
        builder.Append(
            $"LOAD CSV WITH HEADERS FROM '{filename}' AS {l} " +
            $"FIELDTERMINATOR '{csvDelimiter}' " +
            $"MERGE ({node}:{labels} {{{Props.ScriptAddress.GetSetter()}}}) ");

        builder.Append("SET ");
        builder.Append(string.Join(
            ", ",
            from x in _properties where x != Props.ScriptAddress select $"{x.GetSetter(node)}"));

        return builder.ToString();
    }
}
