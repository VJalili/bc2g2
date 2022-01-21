using BC2G.CLI;
using BC2G.Exceptions;
using BC2G.Graph;
using BC2G.Logging;
using BC2G.Model;
using BC2G.Serializers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace BC2G
{
    public class Orchestrator : IDisposable
    {
        public string LogFile { private set; get; }

        public ConcurrentQueue<BlockStatistics> BlocksStatistics { set; get; } = new();

        private readonly HttpClient _client;

        private Logger _logger;
        private readonly string _loggerRepository;
        private const string _defaultLoggerRepoName = "EventsLog";
        private readonly string _loggerTimeStampFormat = "yyyyMMdd_HHmmssfffffff";

        private bool disposed = false;

        private string _statusFilename;
        private readonly Options _status;

        public Orchestrator(string[] args, HttpClient client)
        {
            if (!TryParseArgs(args, out _status))
                throw new Exception();

            _loggerRepository =
                _defaultLoggerRepoName + "_" +
                DateTime.Now.ToString(
                    _loggerTimeStampFormat,
                    CultureInfo.InvariantCulture);
            LogFile = Path.Join(_status.OutputDir, _loggerRepository + ".txt");

            _client = client;
            if (!EnsureOutputDirectory())
                throw new Exception();

            if (!TrySetupLogger())
                throw new Exception();
        }

        public async Task<bool> RunAsync(
            CancellationToken cancellationToken,
            int? from = null, int? to = null)
        {
            if (!TryGetBitCoinAgent(out var agent))
                return false;

            if (!AssertChain(agent, out ChainInfo chaininfo))
                return false;

            if (cancellationToken.IsCancellationRequested)
                return false;

            if (_status.FromInclusive == -1)
                _status.FromInclusive = _status.LastProcessedBlock + 1;
            if (_status.ToExclusive == -1)
                _status.ToExclusive = chaininfo.Blocks;

            if (_status.ToExclusive > _status.FromInclusive)
            {
                try
                {
                    await TraverseBlocksAsync(agent, cancellationToken);
                    _logger.Log(
                        "All process finished successfully.",
                        newLine: true,
                        color: ConsoleColor.Green);
                }
                catch (Exception e)
                {
                    _logger.LogException(e);
                    return false;
                }
            }
            else
            {
                _logger.LogWarning(
                    $"The Start block height must be smaller than the end " +
                    $"block height: `{from}` is not less than `{to}`.");
                return false;
            }

            return true;
        }

        private bool TryParseArgs(string[] args, out Options status)
        {
            try
            {
                var cliOptions = new CommandLineOptions();
                status = cliOptions.Parse(args, out bool helpIsDisplayed);
                _statusFilename = cliOptions.StatusFilename;
                if (helpIsDisplayed)
                    return false;

                if (cliOptions.ResumeFrom != null)
                {
                    try
                    {
                        _statusFilename = cliOptions.ResumeFrom;
                        status = JsonSerializer<Options>
                            .DeserializeAsync(cliOptions.ResumeFrom).Result;
                    }
                    catch (Exception e)
                    {
                        Logger.LogExceptionStatic(
                            $"Failed loading status from " +
                            $"`{cliOptions.ResumeFrom}`: {e.Message}");
                        return false;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                status = new Options();
                if (_logger == null)
                    Logger.LogExceptionStatic(e.Message);
                else
                    _logger.LogException(e);
                Environment.ExitCode = 1;
                return false;
            }
        }

        private bool EnsureOutputDirectory()
        {
            try
            {
                Directory.CreateDirectory(_status.OutputDir);

                var tmpFile = Path.Combine(_status.OutputDir, "tmp_access_test");
                File.Create(tmpFile).Dispose();
                File.Delete(tmpFile);
                return true;
            }
            catch (Exception e)
            {
                Logger.LogExceptionStatic(
                    $"Require write access to the path {_status.OutputDir}: " +
                    $"{e.Message}");

                return false;
            }
        }

        private bool TrySetupLogger()
        {
            try
            {
                _logger = new Logger(
                    LogFile, _loggerRepository, Guid.NewGuid().ToString(),
                    _status.OutputDir, "2GB");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogExceptionStatic($"Logger setup failed: {e.Message}");
                return false;
            }
        }

        private bool TryGetBitCoinAgent(out BitcoinAgent agent)
        {
            // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8625
            agent = null;
#pragma warning restore CS8625

            try
            {
                agent = new BitcoinAgent(_client, _logger);
                if (!agent.IsConnected)
                    throw new ClientInaccessible();
                return true;
            }
            catch (Exception e)
            {
                _logger.LogException(
                    $"Failed to create/access BitcoinAgent: " +
                    $"{e.Message}");
                return false;
            }
        }

        private bool AssertChain(BitcoinAgent agent, out ChainInfo chainInfo)
        {
            chainInfo = new ChainInfo();

            try
            {
                chainInfo = agent.GetChainInfoAsync().Result;
                if (string.IsNullOrEmpty(chainInfo.Chain))
                {
                    _logger.LogException(
                        "Received empty string as chain name " +
                        "from the chaininfo endpoint.");
                    return false;
                }

                if (chainInfo.Chain != "main")
                {
                    _logger.LogException(
                        $"Required to be on the `main` chain, " +
                        $"but the bitcoin client is on the " +
                        $"`{chainInfo.Chain}` chain.");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                _logger.LogException($"Failed getting chain info: {e.Message}");
                return false;
            }
        }

        private async Task TraverseBlocksAsync(
            BitcoinAgent agent, CancellationToken cancellationToken)
        {
            var graphsBuffer = new ConcurrentQueue<GraphBase>();

            var individualBlocksDir = Path.Combine(_status.OutputDir, "individual_blocks");
            if (!Directory.Exists(individualBlocksDir))
                Directory.CreateDirectory(individualBlocksDir);

            using var mapper = new AddressToIdMapper(_status.AddressIdMappingFilename);
            using var txCache = new TxCache(_status.OutputDir);
            using var serializer = new CSVSerializer(mapper);

            // Parallelizing block traversal has more disadvantages than
            // advantages it could bring. One draw back is related to
            // caching transactions, where of a block are cached for faster
            // processing of subsequent blocks, however, when parallelized
            // this optimization may not be fully applicable. For instance,
            // consider traversing blocks 0-200 and the parallelizer
            // partitioner to split the interval to thread_1: 0-100 and
            // thread_2: 100-200. If blocks 100-200 are referencing
            // transactions in blocks 0-100, those transactions may not
            // be in cache yet, hence, BitCoinAgent will need to query
            // Bitcoin client for those transactions, which is considerably
            // slower than using the built-in cache. Partitioner can be
            // adjusted to assign threads as thread_1:0, thread_2:1,
            // thread_1:2, thread_2:3, and so on. This might suffer less
            // than the previous partitioning, and a good techniuqe to
            // implement it is TPL. However, it needs to be tested
            // if/how-much performance optimization it delivers and if
            // it balaces with complications of implementing it.

            _logger.Log(
                $"Traversing blocks [{_status.FromInclusive:n0}, " +
                $"{_status.ToExclusive:n0}):", newLine: true);
            _logger.CursorTop = Console.CursorTop;

            for (int height = _status.FromInclusive; height < _status.ToExclusive; height++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                _logger.LogStartProcessingBlock(height);
                var blockStats = new BlockStatistics(height);

                _logger.LogStatusProcessingBlock(BlockProcessStatus.GetBlockHash);
                var blockHash = await agent.GetBlockHash(height);
                _logger.LogStatusProcessingBlock(BlockProcessStatus.GetBlockHash, false, stopwatch.Elapsed.TotalSeconds);

                if (cancellationToken.IsCancellationRequested)
                    break;

                _logger.LogStatusProcessingBlock(BlockProcessStatus.GetBlock);
                var block = await agent.GetBlock(blockHash);
                _logger.LogStatusProcessingBlock(BlockProcessStatus.GetBlock, false, stopwatch.Elapsed.TotalSeconds);

                if (cancellationToken.IsCancellationRequested)
                    break;

                _logger.LogStatusProcessingBlock(BlockProcessStatus.ProcessTransactions);
                var graph = await agent.GetGraph(block, txCache, cancellationToken);
                _logger.LogStatusProcessingBlock(BlockProcessStatus.ProcessTransactions, false, stopwatch.Elapsed.TotalSeconds);

                if (cancellationToken.IsCancellationRequested)
                    break;

                graphsBuffer.Enqueue(graph);

                _logger.LogStatusProcessingBlock(BlockProcessStatus.Serialize);
                serializer.Serialize(graph, Path.Combine(individualBlocksDir, $"{height}"), blockStats);
                _status.LastProcessedBlock = height;
                await JsonSerializer<Options>.SerializeAsync(_status, _statusFilename);
                _logger.LogStatusProcessingBlock(BlockProcessStatus.Serialize, false, stopwatch.Elapsed.TotalSeconds);

                stopwatch.Stop();
                blockStats.Runtime = stopwatch.Elapsed;
                BlocksStatistics.Enqueue(blockStats);

                _logger.LogFinishProcessingBlock(height, blockStats.Runtime.TotalSeconds);
            }

            var graphsBufferFilename = Path.Combine(_status.OutputDir, "edges.csv");
            _logger.Log($"Serializing all edges in `{graphsBufferFilename}`.", newLine: true);
            serializer.Serialize(graphsBuffer, graphsBufferFilename);

            _logger.Log("Serializing block status", newLine: true);
            BlocksStatisticsSerializer.Serialize(
                BlocksStatistics,
                Path.Combine(_status.OutputDir, "blocks_stats.tsv"));

            // At this method's exist, the dispose method of
            // the types wrapped in `using` will be called that
            // finalizes persisting output.
            _logger.Log("Finalizing serialized files.", newLine: true);
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
                    _logger.Dispose();

                disposed = true;
            }
        }
    }
}
