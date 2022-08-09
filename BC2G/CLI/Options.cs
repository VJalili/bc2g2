﻿using BC2G.DAL;

namespace BC2G.CLI
{
    // TODO: decide on where is a better place to set the
    // defaults and set all the defaults in one place.
    // Currently defaults are set in different places
    // including here and CLI. 

    public class Options
    {
        public int FromInclusive { get; set; } = -1;
        public int ToExclusive { get; set; } = -1;
        public int LastProcessedBlock
        {
            get
            {
                if (_lastProcessedBlock == -1)
                    return FromInclusive == -1 ? -1 : FromInclusive - 1;
                else
                    return _lastProcessedBlock;
            }
            set
            {
                _lastProcessedBlock = value;
            }
        }
        private int _lastProcessedBlock = -1;

        public int Granularity
        {
            get { return _granularity; }
            set { _granularity = value > 0 ? value : 1; }
        }
        private int _granularity = 1;

        public DirectoryInfo WorkingDir { set; get; } = new DirectoryInfo(Environment.CurrentDirectory);
        public int GraphSampleCount { set; get; }
        public GraphSampleMode GraphSampleMode { set; get; } = GraphSampleMode.A;

        public string AddressIdMappingFilename { set; get; } = "id_to_address_mapping.tsv";
        public bool CreatePerBlockFiles { get; set; } = false;
        public int MaxConcurrentBlocks { get; set; } = 1;// Environment.ProcessorCount / 2;

        public string Neo4jUri { get; set; } =
            Environment.GetEnvironmentVariable("NEO4J_URI") ??
            "bolt://localhost:7687";

        public string Neo4jUser { get; set; } =
            Environment.GetEnvironmentVariable("NEO4J_USER") ??
            "neo4j";
        public string Neo4jPassword { set; get; } =
            Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ??
            "password";

        public string Neo4jImportDirectory { set; get; } =
            Environment.GetEnvironmentVariable("NEO4J_IMPORTDIRECTORY") ??
            @"C:\Users\Hamed\.Neo4jDesktop\relate-data\dbmss\dbms-ff193aad-d42a-4cf2-97b5-e7fe6b52b161\import";

        public string Neo4jCypherImportPrefix { set; get; } =
            Environment.GetEnvironmentVariable("NEO4J_CYPHERIMPORTPREFIX") ??
            "file:///";

        public string StatusFilename { set; get; }
    }
}
