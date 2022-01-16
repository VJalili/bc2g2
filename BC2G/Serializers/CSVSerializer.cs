﻿using BC2G.Graph;
using BC2G.Model;
using System.Collections.Concurrent;
using System.Text;

namespace BC2G.Serializers
{
    public class CSVSerializer : SerializerBase, IDisposable
    {
        private const string _delimiter = ",";
        private const string _tmpFilenamePostfix = ".tmp";
        private bool disposed = false;

        private readonly string _mapperFilename = string.Empty;
        private readonly List<string> _createdFiles = new();
        private readonly AddressToIdMapper _mapper = new();
        private readonly BlockStatistics _stats;
        private readonly string _outputDir;

        public CSVSerializer() { }

        public CSVSerializer(AddressToIdMapper mapper, BlockStatistics stats)
        {
            _mapper = mapper;
            _stats = stats;
        }

        public override void Serialize(GraphBase g, string baseFilename)
        {
            var nodesFilename = baseFilename + "_nodes.csv" + _tmpFilenamePostfix;
            var edgeFilename = baseFilename + "_edges.csv" + _tmpFilenamePostfix;

            WriteNodes(g, nodesFilename);
            WriteEdges(g, edgeFilename);

            _createdFiles.Add(nodesFilename);
            _createdFiles.Add(edgeFilename);
        }

        public void Serialize(ConcurrentQueue<GraphBase> graphsBuffer, string edgesFilename)
        {
            // TODO: Note that this approach is not using a staging 
            // file to first write to that, and then replace the 
            // staging file with the expected one (as is done by 
            // the other serializer methods). This mainly becuase this
            // approach appends to an existing file, hence if the 
            // existing file is very big, it will endup copying 
            // the big file to a temporary path, add a few lines
            // to the temporary file, then replace the original
            // file with the temporary file. This approach is not
            // efficient, hence it would need a better/optimized alternative.
            var csvBuilder = new StringBuilder();
            foreach (var g in graphsBuffer)
                foreach (var edge in g.Edges)
                    csvBuilder.AppendLine(
                        string.Join(_delimiter, new string[]
                        {
                            _mapper.GetId(edge.Source).ToString(),
                            _mapper.GetId(edge.Target).ToString(),
                            edge.Value.ToString(),
                            ((byte)edge.Type).ToString(),
                            edge.Timestamp.ToString()
                        }));
            
            File.WriteAllText(edgesFilename, csvBuilder.ToString());
        }


        public override GraphBase Deserialize(string path, int blockHeight)
        {
            var nodeIds = ReadNodes(Path.Combine(path, $"{blockHeight}_nodes.csv"));
            return ReadEdges(Path.Combine(path, $"{blockHeight}_edges.csv"), nodeIds);
        }

        private void WriteNodes(GraphBase g, string filename)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(
                string.Join(_delimiter, new string[]
                { "Id", "Label" }));

            foreach (var node in g.Nodes)
                csvBuilder.AppendLine(string.Join(_delimiter, new string[]
                {
                    _mapper.GetId(node).ToString(),
                    node
                }));

            File.WriteAllText(filename, csvBuilder.ToString());
        }

        private static Dictionary<string, string> ReadNodes(string filename)
        {
            var nodeIds = new Dictionary<string, string>();
            using var reader = new StreamReader(filename);
            string? line;
            reader.ReadLine(); // skip the header.
            while ((line = reader.ReadLine()) != null)
            {
                var x = line.Split(_delimiter);
                nodeIds.Add(x[0], x[1]);
            }

            return nodeIds;
        }

        private void WriteEdges(GraphBase g, string filename)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(
                string.Join(_delimiter, new string[]
                { "Source", "Target", "Weight", "EdgeType", "Timestamp" }));

            // Do NOT refactor "EdgeType" to "Type", since
            // some tools such as Gephi will fail to visualize
            // the graph if this is labeled as "Type".

            foreach (var edge in g.Edges)
            {
                csvBuilder.AppendLine(
                    string.Join(_delimiter, new string[]
                    {
                        _mapper.GetId(edge.Source).ToString(),
                        _mapper.GetId(edge.Target).ToString(),
                        edge.Value.ToString(),
                        ((int)edge.Type).ToString(),
                        edge.Timestamp.ToString()
                    }));

                _stats.IncrementEdgeType(edge.Type);
            }

            File.WriteAllText(filename, csvBuilder.ToString());
        }

        private static GraphBase ReadEdges(string filename, Dictionary<string, string> nodeIds)
        {
            var g = new GraphBase();
            string? line;
            using var reader = new StreamReader(filename);
            reader.ReadLine(); // skip the header.
            while ((line = reader.ReadLine()) != null)
            {
                var x = line.Split(_delimiter);
                g.AddEdge(new Edge(
                    nodeIds[x[0]],
                    nodeIds[x[1]],
                    double.Parse(x[2]),
                    (EdgeType)int.Parse(x[3]),
                    uint.Parse(x[4])));
            }

            return g;
        }

        // The IDisposable interface is implemented following .NET docs:
        // https://docs.microsoft.com/en-us/dotnet/api/system.idisposable?view=net-6.0
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    /// rename temporary files since the 
                    /// method is executed successfully.
                    /// Rename pattern:
                    ///     from: abc.csv.tmp
                    ///       to: abc.csv
                    foreach (var filename in _createdFiles)
                        File.Move(
                            filename,
                            filename[..filename.LastIndexOf(_tmpFilenamePostfix)],
                            true);
                }

                disposed = true;
            }
        }
    }
}
