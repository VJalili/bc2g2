namespace BC2G.CLI.Config;

public class Neo4jOptions
{
    public string Uri { init; get; } =
        Environment.GetEnvironmentVariable("NEO4J_URI") ??
        "bolt://localhost:7687";

    public string User { init; get; } =
        Environment.GetEnvironmentVariable("NEO4J_USER") ??
        "neo4j";

    public string Password { init; get; } =
        Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ??
        "password";

    public string ImportDirectory { init; get; } =
        Environment.GetEnvironmentVariable("NEO4J_IMPORTDIRECTORY") ??
        @"E:\neo4j\relate-data\dbmss\dbms-6e7d8509-ccd6-4124-a28d-d17f254854df\import";

    public string BatchesFilename { init; get; } = "batches.json";

    public string CypherImportPrefix { init; get; } =
        Environment.GetEnvironmentVariable("NEO4J_CYPHERIMPORTPREFIX") ??
        "file:///";
}
