using bitcoin_data.Exceptions;
using bitcoin_data.Model;
using bitcoin_data.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bitcoin_data
{
    public class Orchestrator
    {
        /// <summary>
        /// Should be Fully quantifying path
        /// </summary>
        public string AddressIdFilename { set; get; } = "address_id.csv";
        /// <summary>
        /// Should be fully quantifying path
        /// </summary>
        public string StatusFilename { set; get; } = "status.json";

        private readonly string _outputDir;

        public Orchestrator(string outputDir)
        {
            _outputDir = outputDir;
            AddressIdFilename = Path.Combine(
                _outputDir, AddressIdFilename);
            StatusFilename = Path.Combine(
                _outputDir, StatusFilename);
        }

        public async Task Run()
        {
            CanWriteToOutputDir();
            var status = await LoadStatus();
            var agent = new BitcoinAgent();

            if (!agent.IsConnected)
                throw new ClientInaccessible();

            var chaininfo = await AssertChain(agent);

            if (chaininfo.Blocks > status.LastBlockHeight)
                await TraverseBlocks(
                    agent,
                    status,
                    status.LastBlockHeight + 1,
                    chaininfo.Blocks);
        }

        private void CanWriteToOutputDir()
        {
            try
            {
                var tmpFile = Path.Combine(_outputDir, "tmp_access_test");
                File.Create(tmpFile).Dispose();
                File.Delete(tmpFile);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Require write access to the path {_outputDir}: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private async Task<Status> LoadStatus()
        {
            Status status = new();
            try
            {
                status = await JsonSerializer<Status>.DeserializeAsync(StatusFilename);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
            return status;
        }

        private async Task<ChainInfo> AssertChain(BitcoinAgent agent)
        {
            var chaininfo = await agent.GetChainInfoAsync();
            if (string.IsNullOrEmpty(chaininfo.Chain))
                throw new Exception(
                    "Received empty string as chain name " +
                    "from the chaininfo endpoint.");
            else if (chaininfo.Chain != "main")
                throw new Exception(
                    $"Required to be on the `main` chain, " +
                    $"the bitcoin client is on the " +
                    $"`{chaininfo.Chain}` chain.");
            return chaininfo;
        }

        private async Task TraverseBlocks(BitcoinAgent agent, Status status, int from, int to)
        {
            for (int height = from; height < to; height++)
            {
                var blockHash = await agent.GetBlockHash(height);
                var block = await agent.GetBlock(blockHash);
                var graph = await agent.GetGraph(block);
                var serializer = new CSVSerializer(AddressIdFilename);
                serializer.Serialize(graph,  Path.Combine(_outputDir, $"{height}"));
                status.LastBlockHeight += 1;
                await JsonSerializer<Status>.SerializeAsync(status, StatusFilename);
            }
        }
    }
}
