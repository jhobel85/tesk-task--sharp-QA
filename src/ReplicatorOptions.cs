public class ReplicatorOptions
{
    public const string PREFIX = "/home/croymen/Veeam/tesk-task--sharp-QA/";
    public const string DefaultLogPath = PREFIX + "logs/app.log";
    public const string DefaultSourcePath = PREFIX + "data/source/";
    public const string DefaultReplicaPath = PREFIX + "data/replica/";

    public static readonly TimeSpan DefaultSyncInterval = TimeSpan.FromSeconds(10);   

    public string SourcePath { get; set; } = DefaultSourcePath;
    public string ReplicaPath { get; set; } = DefaultReplicaPath;
    public string LogFilePath { get; set; } = DefaultLogPath;    
    public TimeSpan SyncInterval{get; set;}  = DefaultSyncInterval;
}

