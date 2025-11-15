using ReplicaTool.Interfaces;
namespace ReplicaTool.Configuration
{
    public class ReplicatorOptions : IReplicatorOptions
    {        
        public const string DefaultLogPath = "logs/app.log";
        public const string DefaultSourcePath = "data/source/";
        public const string DefaultReplicaPath = "data/replica/";

        public static readonly TimeSpan DefaultSyncInterval = TimeSpan.FromSeconds(10);

        public string SourcePath { get; set; } = DefaultSourcePath;
        public string ReplicaPath { get; set; } = DefaultReplicaPath;
        public string LogFilePath { get; set; } = DefaultLogPath;
        public TimeSpan SyncInterval { get; set; } = DefaultSyncInterval;

        public bool ArgumentsProvided()
        {
            return true;    //default options have already arguments provided
        }
    }
}
