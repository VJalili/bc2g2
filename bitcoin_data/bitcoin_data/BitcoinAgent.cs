using bitcoin_data.Exceptions;
using bitcoin_data.Model;
using System.Text.Json;

namespace bitcoin_data
{
    internal class BitcoinAgent
    {
        /// <summary>
        /// Sets and gets the REST API endpoint of the Bitcoin client.
        /// </summary>
        public Uri BaseUri { set; get; } = new Uri("http://127.0.0.1:8332/rest/");

        private readonly HttpClient _client;

        public BitcoinAgent()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.UserAgent.Clear();
            _client.DefaultRequestHeaders.Add("User-Agent", "BitcoinAgent");
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
            return 
                await JsonSerializer.DeserializeAsync<Transaction>(
                    await GetResource("tx", hash)) 
                ?? throw new Exception("Invalid transaction.");
        }

        public bool TryGetMinerRewardAddress(Block block, out string address)
        {
            address = string.Empty;
            foreach(var tx in block.Transactions)
            {
                foreach(var input in tx.Inputs)
                {
                    if (!string.IsNullOrEmpty(input.Coinbase))
                        {
                        
                    }
                }
            }


            return false;
        }

        public async Task<Graph> GetGraph(Transaction transaction)
        {
            var graph = new Graph(transaction.Inputs.Count, transaction.Outputs.Count);
            foreach (var input in transaction.Inputs)
            {
                if (!string.IsNullOrEmpty(input.Coinbase))
                {
                    graph.AddSource("Coinbase", -1);
                }
                else if (input.TransactionId != null)
                {
                    // Extended transaction: details of the transaction are retrieved from the bitcoin client.
                    var exTx = await GetTransaction(input.TransactionId);
                    var vout = exTx.Outputs.First(x => x.Index == input.OutputIndex);
                    if (vout != null)
                        graph.AddSource(vout.GetAddress(), vout.Value);
                    else
                    {
                        // TODO: check when either can be null.
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    // TODO: check if this is ever possible.
                    throw new NotImplementedException();
                }
            }

            foreach(var output in transaction.Outputs)
            {
                /*
                if (output.GetScriptType() is
                    ScriptType.Unknown or ScriptType.NullData)
                    continue;*/

                // Ideally the above should be sufficient, but
                // for dev purposes, we use the following.
                if (output.GetScriptType() is ScriptType.NullData)
                    continue;
                if(output.GetScriptType() is ScriptType.Unknown)
                    throw new NotImplementedException();


                graph.AddTarget(output.GetAddress(), output.Value);
            }

            // TODO: exclude change transaction.

            graph.UpdateEdges();
            return graph;
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

            
            /*
            try
            {
                //var response = await _client.GetAsync(_url + "block/000000000000000000056306b6cce2288a61fcfd302714bdbcd4d1d0449db3d9.json");
                //response.EnsureSuccessStatusCode();
                //var res = await response.Content.ReadAsStringAsync();

                // or:
                //var resbody = await _client.GetStringAsync(_url + "block/000000000000000000056306b6cce2288a61fcfd302714bdbcd4d1d0449db3d9.json");

                return _client.GetStreamAsync(new Uri(BaseUri, endpoint));
                //"block/000000000000000000056306b6cce2288a61fcfd302714bdbcd4d1d0449db3d9.json");
                var block = await JsonSerializer.DeserializeAsync<Block>(await stream);

            }
            catch (Exception ex)
            {

            }*/
        }
    }
}
