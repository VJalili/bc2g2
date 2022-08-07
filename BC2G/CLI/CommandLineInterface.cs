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
        private readonly RootCommand rootCmd = new("TODO: some description ...");

        public CommandLineInterface(Func<DirectoryInfo, int, string?, Task> SampleCmdHandler)
        {
            var sampleCmd = GetSampleCommand(SampleCmdHandler);
            rootCmd.AddCommand(sampleCmd);
            rootCmd.AddCommand(GetTraverseCmd());
        }

        public async Task InvokeAsync(string[] args)
        {
            await rootCmd.InvokeAsync(args);
        }

        private static Command GetSampleCommand(Func<DirectoryInfo, int, string?, Task> handler)
        {
            var countOption = new Option<int>(
                name: "--count",
                description: "The number of graphs to sample.");

            var outputDirOption = new Option<DirectoryInfo>(
                name: "--output",
                description: "The directory to store the sampled graph(s).");

            var modeOption = new Option<string?>(
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

            var cmd = new Command(
                name: "sample",
                description: "TODO: add some description")
            {
                countOption,
                outputDirOption,
                modeOption
            };

            cmd.SetHandler(async (outputDir, count, mode) =>
            {
                await handler(outputDir, count, mode);
            },
            outputDirOption, countOption, modeOption);

            return cmd;
        }

        private static Command GetTraverseCmd()
        {
            var cmd = new Command(
                name: "traverse",
                description: "TODO: add some description");
            cmd.AddCommand(GetBitcoinCmd());

            return cmd;
        }

        private static Command GetBitcoinCmd()
        {
            var fromOption = new Option<int>(
                name: "--from",
                description: "The inclusive height of the block where the traverse should start.");

            var toOption = new Option<int>(
                name: "--to",
                description: "The exclusive height of the block where the traverse should end (exclusive).");

            var statusFilenameOption = new Option<FileInfo?>(
                name: "--status-filename",
                description: "The JSON file to store the execution status.",
                isDefault: true,
                parseArgument: x =>
                {
                    if (x.Tokens.Count == 0)
                        return new FileInfo("abc.json"); // TODO: fixme. 

                    var filePath = x.Tokens.Single().Value;
                    return new FileInfo(filePath);
                });

            var resumeFromOption = new Option<int>(
                name: "--resume-from",
                description: "Resumes a canceled execution based on the given status filename.");

            var granularityOption = new Option<int>(
                name: "--granularity",
                description: "Set the blockchain traversal granularity (default is 1)." +
                "For instance, if set to `10`, it implies processing every 10 blocks in the blockchain.");

            var cmd = new Command(
                name: "bitcoin",
                description: "TODO ...")
            {
                fromOption,
                toOption,
                statusFilenameOption,
                resumeFromOption,
                granularityOption
            };

            return cmd;
        }
    }
}
