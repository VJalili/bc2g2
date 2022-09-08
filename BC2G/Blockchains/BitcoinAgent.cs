﻿using BC2G.CLI;
using BC2G.Exceptions;
using BC2G.Graph;
using BC2G.Logging;
using BC2G.Model;
using BC2G.Serializers;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BC2G.Blockchains
{
    public class BitcoinAgent : IDisposable
    {
        public const string coinbase = "Coinbase";
        public static uint GenesisTimestamp { get { return 1231006505; } }

        /// <summary>
        /// Sets and gets the REST API endpoint of the Bitcoin client.
        /// </summary>
        private readonly Uri _baseUri;

        private readonly HttpClient _client;

        private readonly Logger _logger;

        //private readonly TxCache _txCache;

        private readonly CancellationToken _cT;

        private bool _disposed = false;

        //public AddressToIdMapper AddressToIdMapper { get; set; }
        private readonly string _psqlHost;
        private readonly string _psqlDatabase;
        private readonly string _psqlUsername;
        private readonly string _psqlPassword;

        public BitcoinAgent(HttpClient client, Options options, Logger logger, CancellationToken ct)
        {
            _client = client;
            _baseUri = new Uri(options.BitcoinClientUri, "/rest/");

            // the use of Tx cache is disabled since it is not clear 
            // how much improvement it offers to the additional complexity.
            // TODO: needs more experimenting.
            //_txCache = txCache;
            _logger = logger;
            _cT = ct;

            _psqlHost = options.PsqlHost;
            _psqlDatabase = options.PsqlDatabase;
            _psqlUsername = options.PsqlUsername;
            _psqlPassword = options.PsqlPassword;

            using var context = GetDbContext();
            context.Database.EnsureCreated();
        }

        private DatabaseContext GetDbContext()
        {
            return new DatabaseContext(_psqlHost, _psqlDatabase, _psqlUsername, _psqlPassword);
        }

        /// <summary>
        /// Is true if it can successfully query the `chaininfo` endpoint of 
        /// the Bitcoin client via the given value of <paramref name="BaseUri"/>;
        /// false if otherwise.
        /// </summary>
        public async Task<bool> IsConnectedAsync()
        {
            try
            {
                var response = await _client.GetAsync(new Uri(_baseUri, "chaininfo.json"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ChainInfo> GetChainInfoAsync()
        {
            try
            {
                var stream = await SendGet($"chaininfo.json");
                return
                    await JsonSerializer.DeserializeAsync<ChainInfo>(stream)
                    ?? throw new Exception("Error reading chain info.");
            }
            catch (Exception e) when (e is not ClientInaccessible)
            {
                throw new Exception($"Error getting chain info.");
            }
        }

        public async Task<string> GetBlockHash(int height)
        {
            Stream stream;
            try
            {
                stream = await SendGet($"blockhashbyheight/{height}.hex");
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd().Trim();
            }
            catch (Exception e) when (e is not ClientInaccessible)
            {
                throw;
                // The exception can happen when the given block height 
                // is invalid, or the service is not availabe (e.g., the 
                // bitcoin agent is not responding. 
                // throw new Exception($"Invalid height {height}");
            }
        }

        public async Task<Block> GetBlock(string hash)
        {
            /// Implementation note. 
            /// There is a possibility of deadlock in the following code. 
            ///
            ///  return
            ///      await JsonSerializer.DeserializeAsync<Block>(
            ///      await GetResource("block", hash))
            ///      ?? throw new Exception("Invalid block.");
            ///
            /// Therefore, it is replaced by the following. 
            /// This could possibly be improved by first identifying
            /// the corner cases that cause deadlock and try to avoid 
            /// them. 
            /// See the following blog post and SO question for 
            /// implementaiton details.
            ///
            /// https://stackoverflow.com/a/11191070/947889
            /// https://devblogs.microsoft.com/pfxteam/crafting-a-task-timeoutafter-method/

            int millisecondsDelay = 10000;
            var getBlockTask = GetResource("block", hash);
            if (await Task.WhenAny(getBlockTask, Task.Delay(millisecondsDelay, _cT)) == getBlockTask)
            {
                // Re-wating will cause throwing any caugh exception. 
                var blockStream = await getBlockTask;
                return 
                    JsonSerializer.Deserialize<Block>(blockStream) 
                    ?? throw new Exception("Invalid block.");
            }
            else
            {
                throw new ResourceInaccessible(
                    $"Cannot block hash in the given time frame, hash: {hash}");
            }
            /*
            Stream s = null;
            try
            {
                s = await GetResource("block", hash);
            }
            catch(Exception e)
            {
                _logger.Log($"**************** 1 {e.Message} {s}");
            }
            try
            {
                
                await JsonSerializer.DeserializeAsync<Block>(s);
            }
            catch(Exception e)
            {
                _logger.Log($"-------- 2 {hash}; {e.Message}");
            }*/

            /*
            return
                await JsonSerializer.DeserializeAsync<Block>(
                    await GetResource("block", hash))
                ?? throw new Exception("Invalid block.");*/
        }

        public async Task<Transaction> GetTransaction(string hash)
        {
            return
                await JsonSerializer.DeserializeAsync<Transaction>(
                    await GetResource("tx", hash))
                ?? throw new Exception("Invalid transaction.");
        }

        public async Task<BlockGraph> GetGraph(int height)
        {
            // All the logging in this section are disabled because 
            // CPU profiling shows ~%24 of the process time is spent on them.
            if (_cT.IsCancellationRequested) throw new OperationCanceledException();

            //_logger.Log($"Getting block hash; height {height}.");
            var blockHash = await GetBlockHash(height);

            if (_cT.IsCancellationRequested) throw new OperationCanceledException();

            //_logger.Log($"Getting block; height: {height}.");
            var block = await GetBlock(blockHash);

            if (_cT.IsCancellationRequested) throw new OperationCanceledException();

            //_logger.Log($"Getting graph; height: {height}.");
            var graph = new BlockGraph(block);
            await ProcessTxes(graph, block);

            //_logger.Log($"Completed computing graph for block height {height}.");
            return graph;
        }

        private async Task ProcessTxes(BlockGraph g, Block block)
        {
            var utxos = new ConcurrentBag<Utxo>();

            var generationTxGraph = new TransactionGraph();

            // By definition, each block has a generative block that is the
            // reward of the miner. Hence, this should never raise an 
            // exception if the block is not corrupt.
            var coinbaseTx = block.Transactions.First(x => x.IsCoinbase);

            var rewardAddresses = new List<Node>();
            foreach (var output in coinbaseTx.Outputs.Where(x => x.IsValueTransfer))
            {
                output.TryGetAddress(out string address);
                var node = generationTxGraph.AddTarget(
                    new Node(
                        //AddressToIdMapper.GetId(address),
                        address,
                        output.GetScriptType()),
                    output.Value);

                rewardAddresses.Add(node);
                g.Stats.AddInputTxCount(1);


                // TEMP
                //utxos.Add(new Utxo(coinbaseTx.Txid, output.Index, address, output.Value));
                //await AddOrUpdate(new Utxo(coinbaseTx.Txid, output.Index, address, output.Value, block.Height.ToString()) { CreatedInCount = 1 });
                //await _cachedOutputDb.Utxos.AddAsync(new Utxo(coinbaseTx.Txid, output.Index, address, output.Value));
                //_txCache.Add(coinbaseTx.Txid, output.Index, address, output.Value);
                utxos.Add(new Utxo(coinbaseTx.Txid, output.Index, address, output.Value, block.Height.ToString()) { CreatedInCount = 1 });
            }

            g.RewardsAddresses = rewardAddresses;
            g.Enqueue(generationTxGraph);

            // If cancelled, the it will throw the OperationCanceledException
            // which is caught at the orchestrator in order to better handle logging.
            var options = new ParallelOptions() { CancellationToken = _cT };
            await Parallel.ForEachAsync(
                block.Transactions.Where(x => !x.IsCoinbase),
                options,
                async (tx, _loopCancellationToken) =>
                {
                    _loopCancellationToken.ThrowIfCancellationRequested();
                    await ProcessTx(g, tx, utxos);
                });

            await AddOrUpdateRange(utxos);
            /*
            using var context = GetDbContext();
            await context.AddRangeAsync(utxos);
            await context.SaveChangesAsync();*/
        }

        private async Task ProcessTx(BlockGraph g, Transaction tx, ConcurrentBag<Utxo> utxos)
        {
            var txGraph = new TransactionGraph
            {
                Fee = tx.Fee
            };

            _cT.ThrowIfCancellationRequested();

            foreach (var input in tx.Inputs)
            {
                _cT.ThrowIfCancellationRequested();

                double value;
                string address;
                // TODO:
                // creating a separate context for each operation is not ideal, though
                // EF does not currently support concurrency on a context. Therefore, 
                // a context cannot be re-used on multi-thread setup. 
                using var context = GetDbContext();
                var utxo = await context.Utxos.FindAsync(Utxo.GetId(input.TxId, input.OutputIndex));
                if (utxo != null)
                {
                    value = utxo.Value;
                    address = utxo.Address;
                    if (string.IsNullOrEmpty(utxo.ReferencedIn))
                        utxo.ReferencedIn = g.Block.Height.ToString();
                    else
                        utxo.ReferencedIn += g.Block.Height.ToString();
                    utxo.ReferencedInCount++;
                    await context.SaveChangesAsync();
                }
                else
                {
                    /*if (!_txCache.TryGet(
                        input.TxId,
                        input.OutputIndex,
                        out string address,
                        out double value))
                    {*/
                    // Extended transaction: details of the transaction are
                    // retrieved from the bitcoin client.
                    //var exTx = await GetTransaction(input.TxId);
                    var exTx = await GetTransaction(input.TxId);
                    var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                    if (vout == null)
                        // TODO: check when this can be null,
                        // or if it would ever happen.
                        throw new NotImplementedException();

                    vout.TryGetAddress(out address);
                    value = vout.Value;
                    //}
                }

                txGraph.AddSource(
                    new Node(
                        //AddressToIdMapper.GetId(address),
                        address,
                        ScriptType.Unknown), /* TODO: can this set to a better value? */
                    value);
            }

            foreach (var output in tx.Outputs.Where(x => x.IsValueTransfer))
            {
                _cT.ThrowIfCancellationRequested();

                output.TryGetAddress(out string address);
                txGraph.AddTarget(
                    new Node(
                        //AddressToIdMapper.GetId(address),
                        address,
                        output.GetScriptType()),
                    output.Value);

                // TEMP
                //utxos.Add(new Utxo(tx.Txid, output.Index, address, output.Value));
                //_txCache.Add(tx.Txid, output.Index, address, output.Value);
                //await AddOrUpdate(new Utxo(tx.Txid, output.Index, address, output.Value, g.Block.Height.ToString()) { CreatedInCount = 1 });
                utxos.Add(new Utxo(tx.Txid, output.Index, address, output.Value, g.Block.Height.ToString()) { CreatedInCount = 1 });

            }

            g.Stats.AddInputTxCount(tx.Inputs.Count);
            g.Stats.AddOutputTxCount(tx.Outputs.Count);
            g.Enqueue(txGraph);
        }

        // Based on the cpu profiling, this method takes most of the cpu time (about ~%20). 
        private async Task AddOrUpdateRange(ConcurrentBag<Utxo> utxos)
        {
            try
            {
                using var c = GetDbContext();
                await c.Utxos.AddRangeAsync(utxos);
                await c.SaveChangesAsync();
            }
            catch (DbUpdateException e)
            when (e.InnerException is PostgresException pe && (pe.SqlState == "23505"))
            {
                // A list of the error codes are available in the following page.
                // https://www.postgresql.org/docs/current/errcodes-appendix.html
                //
                // - 23505: unique_violation (when adding an entity whose indexed property is already defined).

                using var c = GetDbContext();
                foreach (var utxo in utxos)
                {
                    var existingUtxo = c.Utxos.Find(utxo.Id);
                    if (existingUtxo == null)
                    {
                        await c.Utxos.AddAsync(utxo);
                    }
                    else
                    {
                        existingUtxo.CreatedIn += ";" + utxo.CreatedIn;
                        existingUtxo.CreatedInCount++;
                    }
                }
                await c.SaveChangesAsync();
            }
        }

        private async Task<Stream> GetResource(string endpoint, string hash)
        {
            return await SendGet($"{endpoint}/{hash}.json");
        }

        private async Task<Stream> SendGet(string endpoint, int maxRetries = 3)
        {
            try
            {
                return await _client.GetStreamAsync(
                    new Uri(_baseUri, endpoint));
            }
            catch (TaskCanceledException e)
            {
                //_logger.Log($"-------- 2 {endpoint}; {e.Message}");
                // Cancelation triggered by the user. 
                if (e.CancellationToken == _cT)
                    throw e;

                if (maxRetries >= 1)
                {
                    return await SendGet(endpoint, --maxRetries);
                }
                else
                {
                    var msg = e.Message;
                    if (e.InnerException != null)
                        msg += "Cannot make the request after 3 tries: " + e.InnerException.Message;
                    throw new TaskCanceledException(msg);
                }
            }
            catch (HttpRequestException e)
            {
                //_logger.Log($"-------- 3 {endpoint}; {e.Message}");
                if (maxRetries >= 1)
                {
                    return await SendGet(endpoint, --maxRetries);
                }
                else
                {
                    var msg = e.Message;
                    if (e.InnerException != null)
                        msg += "Cannot make the request after 3 tries: " + e.InnerException.Message;
                    throw new HttpRequestException(msg);
                }
            }
            catch (Exception e)
            {
                var connected = await IsConnectedAsync();
                if (!connected)
                {
                    if (maxRetries >= 1)
                        return await SendGet(endpoint, --maxRetries);

                    //_logger.Log($"-------- 5 inaccessible {endpoint}");
                    throw new ClientInaccessible(
                        "Failed to connect to the Bitcoin client after 3 tries. ");
                }

                //_logger.Log($"-------- 4 {endpoint}; {e.Message}");
                var msg = e.Message;
                if (e.InnerException != null)
                    msg += " " + e.InnerException.Message;
                throw new Exception(msg);
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    //_txCache.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
