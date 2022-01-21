using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.CLI
{
    public class CommandLineOptions
    {
        private readonly CommandLineApplication _cla;

        private readonly CommandOption _fromOption = new(
            "-f | --from <value>",
            CommandOptionType.SingleValue)
        {
            Description = "Sets the height of the block where " +
            "the traverse should start (inclusive)."
        };

        private readonly CommandOption _toOption = new(
            "-t | --to <value>",
            CommandOptionType.SingleValue)
        {
            Description = "Sets the height of the block where " +
            "the traverse should end (exclusive)."
        };

        private readonly CommandOption _outputOption = new(
            "-o | --output <value>", 
            CommandOptionType.SingleValue)
        {
            Description = "Sets a path where the result of " +
            "block traversal should be persisted."
        };

        private int _from;
        private int _to;
        private string _output = string.Empty;

        public static string HelpOption
        {
            get { return "-? | -h | --help"; }
        }

        public CommandLineOptions()
        {
            _cla = new CommandLineApplication
            {
                Name = "BC2G",
                FullName = "bitcoin Chain to Graph",
            };

            _cla.Options.Add(_fromOption);
            _cla.Options.Add(_toOption);
            _cla.Options.Add(_outputOption);

            var version = "Unknown (Called from unmanaged code)";
            if (Assembly.GetEntryAssembly() != null)
                #pragma warning disable CS8604 // Possible null reference argument.
                #pragma warning disable CS8602 // Dereference of a possibly null reference.
                version = Assembly.GetEntryAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    .InformationalVersion;
                #pragma warning restore CS8602 // Dereference of a possibly null reference.
                #pragma warning restore CS8604 // Possible null reference argument.

            _cla.HelpOption(HelpOption);
            _cla.VersionOption("-v | --version", () =>
            {
                return $"Version {version}";
            });

            Func<int> assertArguments = AssertArguments;
            _cla.OnExecute(assertArguments);
        }

        public Status Parse(string[] args, out bool helpIsDisplayed)
        {
            helpIsDisplayed = _cla.Execute(args) != 1;
            return new Status()
            {
                FromInclusive = _from,
                ToExclusive = _to
            };
        }

        private int AssertArguments()
        {
            AssertRequiredArgsAreGiven();
            return 1;
        }

        private void AssertRequiredArgsAreGiven()
        {
            var missingArgs = new List<string>();
            // Any required argument?! 
            // For any required argument, check if a value is provided
            // and if not, add the argument to the missingArgs list.

            if (missingArgs.Count > 0)
            {
                var msgBuilder = new StringBuilder(
                    "the following required arguments are missing: ");
                for (int i = 0; i < missingArgs.Count - 1; i++)
                    msgBuilder.Append(missingArgs[i] + "; and ");
                msgBuilder.Append(missingArgs[^1] + ".");

                throw new ArgumentException(msgBuilder.ToString());
            }
        }
    }
}
