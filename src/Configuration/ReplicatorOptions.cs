using ReplicaTool.Interfaces;
namespace ReplicaTool.Configuration
{
    public class ReplicatorOptions() : IReplicatorOptions
    {        
        public string SourcePath { get; set; } = Path.Combine("data", "source") + Path.DirectorySeparatorChar;
        public string LogFilePath { get; set; } = Path.Combine("logs", "app.log");
        public string ReplicaPath { get; set; } = Path.Combine("data", "replica") + Path.DirectorySeparatorChar;
        public TimeSpan SyncInterval { get; set; } = TimeSpan.FromSeconds(5);
        public bool ArgumentsProvided() => true;
    }
}
