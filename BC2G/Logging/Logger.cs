using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace BC2G.Logging
{
    public class Logger : IDisposable
    {
        public string MaxLogFileSize { get; } = "2GB";

        private readonly ILog log;
        private readonly string _name;
        private readonly string _repository;

        private readonly MovingAverage _runtimeMovingAverage;

        private bool disposed = false;

        private readonly string[] _messages;

        public Logger(
            string logFilename, string repository,
            string name, string exportPath,
            string maxLogFileSize)
        {
            MaxLogFileSize = maxLogFileSize;
            _runtimeMovingAverage = new MovingAverage(10);

            _name = name;
            _repository = repository;
            LogManager.CreateRepository(_repository);
            var hierarchy = (Hierarchy)LogManager.GetRepository(_repository);

            var patternLayout = new PatternLayout
            {
                ConversionPattern = "%date\t[%thread]\t%-5level\t%message%newline"
            };
            patternLayout.ActivateOptions();

            var roller = new RollingFileAppender
            {
                AppendToFile = true,
                File = logFilename,
                Layout = patternLayout,
                MaxSizeRollBackups = 5,
                MaximumFileSize = MaxLogFileSize,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                StaticLogFileName = true
            };
            roller.ActivateOptions();
            hierarchy.Root.AddAppender(roller);

            var memory = new MemoryAppender();
            memory.ActivateOptions();
            hierarchy.Root.AddAppender(memory);

            hierarchy.Root.Level = Level.Info;
            hierarchy.Configured = true;
            log = LogManager.GetLogger(_repository, _name);

            _messages = new string[]
            {
                "Cancelling",
                "Getting block hash\t",
                "Getting block\t",
                "Procesing Transactions",
                "Serializing\t"
            };

            log.Info("NOTE THAT THE LOG PATTERN IS: <Date> <#Thread> <Level> <Message>");
            Log($"Export Directory: {exportPath}", true, ConsoleColor.DarkGray);
        }

        public void Log(string message, bool writeLine = true)
        {
            if (writeLine)
                AsyncConsole.WriteLineAsync(message);
            else
                AsyncConsole.WriteAsync(message);
            
            log.Info(message);
        }

        public void Log(string message, bool writeLine, ConsoleColor color)
        {
            if (writeLine)
                AsyncConsole.WriteLineAsync(message, color);
            else
                AsyncConsole.WriteAsync(message, color);

            log.Info(message);
        }

        public void LogStartProcessingBlock(int blockHeight)
        {
            AsyncConsole.EraseToBookmarkedLine();
            AsyncConsole.WriteLineAsync($"\r{blockHeight}\t ({_runtimeMovingAverage.Speed} B/sec)");
        }

        public void LogFinishProcessingBlock(int blockHeight, double runtime)
        {
            _runtimeMovingAverage.Add(runtime);
            AsyncConsole.WriteLineAsync(
                $"\n  *  Successfully finished processing block in " +
                $"{Math.Round(runtime, 2)} seconds.",
                ConsoleColor.DarkGray);
        }

        public void LogBlockProcessStatus(BlockProcessStatus status, bool started = true, double runtime = 0)
        {
            if (status == BlockProcessStatus.ProcessTransactions && !started)
                AsyncConsole.WriteLineAsync("\r  └  " + _messages[(byte)status] +
                    "\t... " + $"Done ({Math.Round(runtime, 2)} sec)", color: ConsoleColor.DarkGray);
            else if (started)
                AsyncConsole.WriteAsync(
                    "  └  " + _messages[(byte)status] +
                    "\t... ", color: ConsoleColor.DarkGray);
            else
                AsyncConsole.WriteLineAsync(
                    $"Done ({Math.Round(runtime, 2)} sec)",
                    color: ConsoleColor.DarkGray);
        }

        public void LogTransaction(string msg)
        {
            msg = "\r  └  " + _messages[(byte)BlockProcessStatus.ProcessTransactions] + "\t... " + msg;
            AsyncConsole.WriteAsync(msg, color: ConsoleColor.DarkGray);
        }

        public void LogException(Exception e)
        {
            LogException(e.Message);
        }

        public void LogException(string message)
        {
            LogExceptionStatic(message);
            log.Error(message);

            // TODO:
            /*
            log.Info(_linkToDocumentation);
            log.Info(_linkToIssuesPage);
            log.Info(HintHelpMessage);
            log.Info(_cannotContinue);*/
        }

        public static void LogExceptionStatic(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(string.Format("Error: {0}", message));
            Console.ForegroundColor = ConsoleColor.Yellow;
            // TODO:
            // Console.WriteLine(HintHelpMessage);
            Console.ResetColor();
            // TODO:
            // Console.WriteLine(_cannotContinue);
        }

        public void LogWarning(string message)
        {
            log.Warn(message);
            LogWarningStatic(message);
        }

        public static void LogWarningStatic(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"Warning: {message}");
            Console.ResetColor();
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
                {
                    LogManager.Flush(5000);

                    var l = (log4net.Repository.Hierarchy.Logger)LogManager
                        .GetLogger(_repository, _name).Logger;
                    l.RemoveAllAppenders();

                    LogManager.GetLogger(_repository, _name)
                        .Logger.Repository.Shutdown();
                }

                disposed = true;
            }
        }
    }
}
