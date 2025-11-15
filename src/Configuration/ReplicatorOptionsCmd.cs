
using ReplicaTool.Interfaces;

namespace ReplicaTool.Configuration
{
    public class ReplicatorOptionsCmd : IReplicatorOptions
    {
        public string SourcePath { get; private set; } = "";
        public string ReplicaPath { get; private set; } = "";
        public string LogFilePath { get; private set; } = "";
        public TimeSpan SyncInterval { get; private set; } = TimeSpan.Zero;

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
                Console.WriteLine("Error: Not all arguments were provided.");
                PrintArguments();
                Console.WriteLine("Usage: dotnet run --source <path> --replica <path> --log <path> --interval <seconds>");
                Console.WriteLine("Example: dotnet run --source data/source/ --replica data/replica/ --log logs/app.log --interval 10");
            }
            return ret;
        }

        private void PrintArguments()
        {
            Console.WriteLine($"Source: {SourcePath}");
            Console.WriteLine($"Replica: {ReplicaPath}");
            Console.WriteLine($"Log: {LogFilePath}");
            Console.WriteLine($"Interval: {SyncInterval.Seconds} seconds (must be > 0)");
            Console.WriteLine();
        }
    }
}