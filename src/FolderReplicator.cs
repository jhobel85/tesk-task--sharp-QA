using System;
using Serilog;

public class FolderReplicator : IScheduler
{
    public FileManager FileMgr { get; private set; }
    private readonly string sourcePath;
    private readonly string replicaPath;

    public FolderReplicator(IReplicatorOptions options)
    {
        sourcePath = options.SourcePath;
        replicaPath = options.ReplicaPath;
        FileMgr = new FileManager(options.LogFilePath);
    }

    public void Replicate()
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

    public void Update()
    {
        Replicate();
    }
}