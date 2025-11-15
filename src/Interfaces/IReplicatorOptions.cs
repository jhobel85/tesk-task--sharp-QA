namespace ReplicaTool.Interfaces
{
    public interface IReplicatorOptions
    {
        public string SourcePath { get; }
        public string ReplicaPath { get; }
        public string LogFilePath { get; }
        public TimeSpan SyncInterval { get; }
        public bool ArgumentsProvided();
    }
}