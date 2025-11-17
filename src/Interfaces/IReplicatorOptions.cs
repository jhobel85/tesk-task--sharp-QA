namespace ReplicaTool.Interfaces
{
    public interface IReplicatorOptions
    {
        string SourcePath { get; set;}
        string ReplicaPath { get; set; }
        string LogFilePath { get; set; }
        TimeSpan SyncInterval { get; set;}
        bool ArgumentsProvided();
    }
}