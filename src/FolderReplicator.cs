using System;
using Serilog;

public class FolderReplicator
{
    public const string PREFIX = "/home/croymen/Veeam/tesk-task--sharp-QA/";
    public const string DefaultLogPath = PREFIX + "logs/app.log";
    public const string DefaultSourcePath = PREFIX + "data/source/";
    public const string DefaultReplicaPath = PREFIX + "data/replica/";
    public static readonly TimeSpan DefaultSyncInterval = TimeSpan.FromSeconds(10);

    public FileManager FileMgr { get; private set; }

    public FolderReplicator(string? logFilePath = null)
    {
        string logPath = logFilePath ?? DefaultLogPath;
        FileMgr = new FileManager(logPath);
    }

    /*
Run replication once immediately
*/
    public void ReplicateNow(string? sourcePath = null, string? replicaPath = null)
    {        
        string srcPath = sourcePath ?? DefaultSourcePath;
        string rplPath = replicaPath ?? DefaultReplicaPath;
        Replicate(srcPath, rplPath);
    }
    
    /*
Run replication periodically in interval
*/
    public void Replicate(string sourcePath, string replicaPath, TimeSpan? syncInterval)
    {        
        TimeSpan interval = syncInterval ?? DefaultSyncInterval;
        //TODO shedullle runner
        Replicate(sourcePath, replicaPath);        
    }    

    private void Replicate(string sourcePath, string replicaPath)
    {
        //TODO do it for directories
        //Get the list of files 
        string[] destinationFiles = Directory.GetFiles(replicaPath);        
        string[] sourceFiles = Directory.GetFiles(sourcePath);        

        //Copy each source file
        foreach (string file in sourceFiles)
        {
            string destinationFile = Path.Combine(replicaPath, Path.GetFileName(file));
            FileMgr.Copy(file, destinationFile);
        }

        //Cleanup files that are not in source folder
        var filesToDelete = destinationFiles
           .Where(file =>
           {
               string filePath = Path.GetFileName(file);
               bool exists = sourceFiles.Any(path => path.EndsWith(filePath));
               return !exists; // do NOT delete if exists
           }).ToList();

        foreach (string file in filesToDelete)
        {
            FileMgr.Delete(file); 
        }        
    }

}