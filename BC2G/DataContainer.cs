using BC2G.Graph;
using BC2G.Model;
using BC2G.Serializers;
using System.Diagnostics;

namespace BC2G
{
    public class DataContainer
    {
        public Block Block { set; get; }
        public GraphBase GraphBase { set; get; }

        public int BlockHeight { get; }
        public Stopwatch Stopwatch { get; }
        public BlockStatistics BlockStatistics { get; }
        public StreamWriter EdgesStreamWriter { get; }
        public StreamWriter BlockStatsStreamWriter { get; }
        public TxIndex TxCache { get; }
        public AddressToIdMapper Mapper { get; }
        public CancellationToken CancellationToken { get; }

        public DataContainer(
            int blockHeight,
            StreamWriter edgesStreamWriter,
            StreamWriter blockStatsStreamWriter,
            TxIndex txCache,
            AddressToIdMapper mapper,
            CancellationToken cancellationToken)
        {
            BlockHeight = blockHeight;
            EdgesStreamWriter = edgesStreamWriter;
            BlockStatsStreamWriter = blockStatsStreamWriter;
            TxCache = txCache;
            Mapper = mapper;
            CancellationToken = cancellationToken;

            Block = new Block();
            BlockStatistics = new BlockStatistics(BlockHeight);
            Stopwatch = new Stopwatch();

            // This initializatin puts the object on the safer-side
            // because this instance of GraphBase will not be used.
            GraphBase = new GraphBase(BlockStatistics);
        }
    }
}
