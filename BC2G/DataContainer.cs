using BC2G.Graph;
using BC2G.Model;
using BC2G.Serializers;
using System.Diagnostics;

namespace BC2G
{
    public class DataContainer
    {
        public int BlockHeight { get; set; }
        public Stopwatch Stopwatch { get; set; }=new Stopwatch();
        public BlockStatistics BlockStatistics { get; set; }
        public Block Block { get; set; }
        public GraphBase GraphBase { get; set; }
        public StreamWriter EdgesStreamWriter { get; set; }
        public StreamWriter BlockStatsStreamWriter { get; set; }
        public TxIndex TxCache { get; set; }
        public AddressToIdMapper Mapper { set; get; }
        public CancellationToken CancellationToken { get; set; }
    }
}
