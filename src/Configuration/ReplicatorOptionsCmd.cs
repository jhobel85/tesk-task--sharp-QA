
using ReplicaTool.Interfaces;
using ReplicaTool.Common;
using Serilog;

namespace ReplicaTool.Configuration
{
    public class ReplicatorOptionsCmd : IReplicatorOptions
    {
        private readonly ILogger _log = Logger.CLI_LOGGER;
        public string SourcePath { get; set; } = "";
        public string ReplicaPath { get; set; } = "";
        public string LogFilePath { get; set; } = "";
        public TimeSpan SyncInterval { get; set; } = TimeSpan.Zero;

        public static ReplicatorOptionsCmd Parse(string[] args)
        {
            var options = new ReplicatorOptionsCmd();

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--source":
                        options.SourcePath = args.ElementAtOrDefault(++i) ?? "";
                        break;
                    case "--replica":
                        options.ReplicaPath = args.ElementAtOrDefault(++i) ?? "";
                        break;
                    case "--log":
                        options.LogFilePath = args.ElementAtOrDefault(++i) ?? "";
                        break;
                    case "--interval":
                        if (int.TryParse(args.ElementAtOrDefault(++i), out int parsed))
                            options.SyncInterval = TimeSpan.FromSeconds(parsed);
                        break;
                }
            }
            return options;
        }

        public bool ArgumentsProvided()
        {
            bool ret = true;
            // if on of parameters is not set mark it as NOT valid and print syntax to output
            if (string.IsNullOrEmpty(SourcePath) || string.IsNullOrEmpty(ReplicaPath) || string.IsNullOrEmpty(LogFilePath) || SyncInterval <= TimeSpan.Zero)
            {
                ret = false;
                _log.Error("Not all arguments were provided.");
                PrintArguments();
                _log.Information("Usage: dotnet run --source <path> --replica <path> --log <path> --interval <seconds>");
                _log.Information("Example: dotnet run --source data/source/ --replica data/replica/ --log logs/app.log --interval 5");
            }
            return ret;
        }

        private void PrintArguments()
        {
            _log.Information($"Source: {SourcePath}");
            _log.Information($"Replica: {ReplicaPath}");
            _log.Information($"Log: {LogFilePath}");
            _log.Information($"Interval: {SyncInterval.Seconds} seconds (must be > 0)");
            _log.Information("");
        }
    }
}