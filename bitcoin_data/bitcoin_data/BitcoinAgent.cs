using bitcoin_data.Exceptions;
using bitcoin_data.Graph;
using bitcoin_data.Model;
using System.Text.Json;

namespace bitcoin_data
{
    public class BitcoinAgent
    {
        /// <summary>
        /// Sets and gets the REST API endpoint of the Bitcoin client.
        /// </summary>
        public Uri BaseUri { set; get; } = new Uri("http://127.0.0.1:8332/rest/");

        private readonly HttpClient _client;
        private const string coinbaseTxLabel = "Coinbase";

        public BitcoinAgent(HttpClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Is true if it can successfully query the `chaininfo` endpoint of 
        /// the Bitcoin client via the given value of <paramref name="BaseUri"/>;
        /// false if otherwise.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                try
                {
                    var _ = _client.GetAsync(new Uri(BaseUri, "chaininfo.json")).Result;
                    return true;
                }
                catch
                {
                    return false;
                }
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
                throw new Exception($"Invalid height {height}");
            }
        }

        public async Task<Block> GetBlock(string hash)
        {
            return
                await JsonSerializer.DeserializeAsync<Block>(
                    await GetResource("block", hash))
                ?? throw new Exception("Invalid block.");
        }

        public async Task<Transaction> GetTransaction(string hash)
        {
            /*
            try
            {
                
                var baseUri = new System.Uri("http://127.0.0.1:8332/rest/");
                var endpoint = "tx/d1beb926923a47c1f9e9cbb7a2bb4f4bf71160c14728e8c24ef20ea37b19c107.json";
                //var x = _client.GetStreamAsync(new System.Uri(baseUri, endpoint)).Result;
                //var x = SendGet(endpoint).Result;
                var x = GetResource("tx", "d1beb926923a47c1f9e9cbb7a2bb4f4bf71160c14728e8c24ef20ea37b19c107").Result;
                var t = JsonSerializer.DeserializeAsync<Transaction>(x).Result;

                var x1 = GetResource("tx", hash).Result;
                var reader = new StreamReader(x1);
                var y = reader.ReadToEnd();
                
            }
            catch (Exception ex)
            {

            }

            try
            {
                var x2 = GetResource("tx", hash).Result;
                var y2 = JsonSerializer.DeserializeAsync<Transaction>(x2).Result;
            }
            catch (Exception ex)
            {

            }*/



            return 
                await JsonSerializer.DeserializeAsync<Transaction>(
                    await GetResource("tx", hash)) 
                ?? throw new Exception("Invalid transaction.");
        }

        public async Task<BlockGraph> GetGraph(Block block)
        {
            var blockGraph = new BlockGraph();

            /// Why using "mediantime" and not "time"? see the following BIP:
            /// https://github.com/bitcoin/bips/blob/master/bip-0113.mediawiki
            uint timestamp = block.MedianTime;

            /// By definition, each block has a generative block that is the 
            /// reward of the miner. Hence, this should never raise an 
            /// exception if the block is not corrupt.
            var coinbaseTx = block.Transactions.First(x => x.IsCoinbase);
            var coinbaseTxGraph = new CoinbaseTransactionGraph(coinbaseTxLabel, coinbaseTx.Outputs.Count);
            var rewardAddresses = new List<string>();
            foreach (var output in coinbaseTx.Outputs.Where(x => x.IsValueTransfer))
            {
                if (!output.TryGetAddress(out string address))
                    continue;
                rewardAddresses.Add(address);
                coinbaseTxGraph.AddTarget(address, output.Value);
            }
            blockGraph.AddGraph(coinbaseTxGraph);

            foreach (var tx in block.Transactions.Where(x => !x.IsCoinbase))
            {
                var txGraph = new TransactionGraph(tx.Inputs.Count, tx.Outputs.Count, rewardAddresses);
                foreach (var input in tx.Inputs)
                {
                    if (input.TxId != null)
                    {
                        // Extended transaction: details of the transaction are retrieved from the bitcoin client.
                        var exTx = await GetTransaction(input.TxId);
                        var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                        if (vout == null)
                            // TODO: check when this can be null, or if it would ever happen.
                            throw new NotImplementedException();

                        var s = vout.TryGetAddress(out string address);
                        if(s == false)
                        {

                        }
                        txGraph.AddSource(address, vout.Value);
                    }
                    else
                    {
                        // TODO: check if this is ever possible.
                        throw new NotImplementedException();
                    }
                }

                foreach (var output in tx.Outputs.Where(x => x.IsValueTransfer))
                    if (output.TryGetAddress(out string address))
                        txGraph.AddTarget(address, output.Value);

                blockGraph.AddGraph(txGraph);
            }

            // TODO: exclude change transaction.
            return blockGraph;
        }

        private async Task<Stream> GetResource(string endpoint, string hash)
        {
            try
            {
                return await SendGet($"{endpoint}/{hash}.json");
            }
            catch (Exception e) when (e is not ClientInaccessible)
            {
                throw new Exception($"Invalid hash `{hash}`.");
            }
        }

        private async Task<Stream> SendGet(string endpoint)
        {
            try
            {
                return await _client.GetStreamAsync(
                    new Uri(BaseUri, endpoint));
            }
            catch when (!IsConnected)
            {
                throw new ClientInaccessible();
            }
        }
    }
}
