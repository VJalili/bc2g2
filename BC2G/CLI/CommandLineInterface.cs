using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.CommandLine;

namespace BC2G.CLI
{
    internal class CommandLineInterface
    {
        private readonly Option<int> _sampleGraphCount = new(
            name: "--count",
            description: "The number of graphs to sample.");

        private readonly Option<DirectoryInfo> _sampledGraphOutputDirOption = new(
            name: "--output", 
            description: "The directory to store the sampled graph(s).");

        private readonly Option<string?> _sampleModeOption = new(
            name: "--mode",
            description: "Valid values are: " +
            "`A` to generate graph and random edge pairs where the number of random edges equal the number of edges in the graph;" +
            "`B` ",
            isDefault: true,
            parseArgument: x =>
            {
                var value = x.Tokens.Single().Value;
                switch (value)
                {
                    case "A":
                        return value;
                    case "B":
                        return value;
                    default:
                        x.ErrorMessage = $"Invalid mode; provided `{value}`, expected `A` or `B`";
                        return null;
                }
            });

        private readonly RootCommand rootCmd = new("TODO: some description ...");

        public CommandLineInterface(Func<DirectoryInfo, int, string?, Task> SampleCmdHandler)
        {
            var sampleCmd = new Command("sample", "sample graph")
            {
                _sampleGraphCount,
                _sampledGraphOutputDirOption,
                _sampleModeOption
            };
            sampleCmd.SetHandler(async (outputDir, count, mode) =>
            {
                await SampleCmdHandler(outputDir, count, mode);
            }, 
            _sampledGraphOutputDirOption, _sampleGraphCount, _sampleModeOption);


            rootCmd.AddCommand(sampleCmd);
        }

        public async Task InvokeAsync(string[] args)
        {
            await rootCmd.InvokeAsync(args);
        }
    }
}
