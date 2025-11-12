using System;

public class FolderReplicator
{
    private const string PREFIX = "/home/croymen/Veeam/tesk-task--sharp-QA/";
    private const string DefaultLogPath = PREFIX + "logs/app.log";
    private const string DefaultSourcePath = PREFIX + "data/source/";
    private const string DefaultReplicaPath = PREFIX + "data/replica/";
    private static readonly TimeSpan DefaultSyncInterval = TimeSpan.FromSeconds(10);

    private string logPath;

    public FolderReplicator(string? logFilePath = null)
    {
        //TODO init logger       
        logPath = logFilePath ?? DefaultLogPath;
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
        Console.WriteLine("Replica files: " + destinationFiles.Length);
        string[] sourceFiles = Directory.GetFiles(sourcePath);
        Console.WriteLine("Source files: " + sourceFiles.Length);

        if ((new DirectoryInfo(replicaPath)).Attributes.HasFlag(FileAttributes.ReadOnly))
        {
            Console.WriteLine("Replica folder is read-only.");
        }

        //Copy each source file
        foreach (string file in sourceFiles)
        {
            try
            {
                string destinationFile = Path.Combine(replicaPath, Path.GetFileName(file));
                File.Copy(file, destinationFile, overwrite: true);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while copying files.\nSource path: " + sourcePath + "\nReplication path: " + replicaPath);
                Console.WriteLine(e);
            }
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
            try
            {
                File.Delete(file);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while cleaning replication path: " + replicaPath);
                Console.WriteLine(e);
            }
        }

        Console.WriteLine("Replication DONE");
    }

}