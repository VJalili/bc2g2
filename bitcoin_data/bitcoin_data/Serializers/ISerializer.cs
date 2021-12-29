using bitcoin_data.Graph;
using bitcoin_data.Model;

namespace bitcoin_data.Serializers
{
    internal interface ISerializer
    {
        void Serialize(string outputFilename, BlockGraph g, Dictionary<string, int> nodeID);
    }
}
