using BC2G.Graph;
using System.Text;

namespace BC2G.Serializers
{
    public class CSVSerializer : SerializerBase
    {
        private const string _delimiter = ",";

        public CSVSerializer() { }

        public CSVSerializer(string addressToIdMappingsFilename) :
            base(addressToIdMappingsFilename)
        { }

        public override void Serialize(BlockGraph g, string baseFilename)
        {
            WriteNodes(g, baseFilename + "_nodes.csv");
            WriteEdges(g, baseFilename + "_edges.csv");
        }

        public override BlockGraph Deserialize(string path, int blockHeight)
        {
            var nodeIds = ReadNodes(Path.Combine(path, $"{blockHeight}_nodes.csv"));
            return ReadEdges(Path.Combine(path, $"{blockHeight}_edges.csv"), nodeIds);
        }

        private void WriteNodes(BlockGraph g, string filename)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(
                string.Join(_delimiter, new string[]
                {
                    "Id", "Label"
                }));

            foreach (var node in g.Nodes)
                csvBuilder.AppendLine(string.Join(_delimiter, new string[]
                {
                    Mapper.GetId(node).ToString(),
                    node
                }));

            File.WriteAllText(filename, csvBuilder.ToString());
        }

        private static Dictionary<string, string> ReadNodes(string filename)
        {
            var nodeIds = new Dictionary<string, string>();
            using var reader = new StreamReader(filename);
            string? line;
            string[] x;
            reader.ReadLine(); // skip the header.
            while ((line = reader.ReadLine()) != null)
            {
                x = line.Split(_delimiter);
                nodeIds.Add(x[0], x[1]);
            }

            return nodeIds;
        }

        private void WriteEdges(BlockGraph g, string filename)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(
                string.Join(_delimiter, new string[]
                {
                    "Source", "Target", "Weight", "EdgeType" // Do not name this as "Type"
                }));

            foreach (var edge in g.Edges)
                csvBuilder.AppendLine(
                    string.Join(_delimiter, new string[]
                    {
                        Mapper.GetId(edge.Source).ToString(),
                        Mapper.GetId(edge.Target).ToString(),
                        edge.Value.ToString(),
                        ((byte)edge.Type).ToString()
                    }));

            File.WriteAllText(filename, csvBuilder.ToString());
        }

        private static BlockGraph ReadEdges(string filename, Dictionary<string, string> nodeIds)
        {
            var g = new BlockGraph();

            using var reader = new StreamReader(filename);
            string? line;
            string[] x;
            reader.ReadLine(); // skip the header.
            while ((line = reader.ReadLine()) != null)
            {
                x = line.Split(_delimiter);
                g.AddEdge(new Model.Edge(
                    nodeIds[x[0]],
                    nodeIds[x[1]],
                    double.Parse(x[2]),
                    (Model.EdgeType)int.Parse(x[3])));
            }

            return g;
        }
    }
}
