using System;
using Serilog;
using ReplicaTool.Interfaces;
using ReplicaTool.Common;

namespace ReplicaTool.Services
{
    public class FolderReplicator : IReplicator
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
            SyncNewPaths(); // Create or Copy new directories and files
            CleanupReplicaDirectories();
            CleanupReplicaFiles();
        }

        private void SyncNewPaths()
        {
            //Get the list of directories and files
            var sourceDirs = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories);
            var sourceFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);

            //Ensure directories exists
            FileMgr.CreateDir(replicaPath);
            foreach (string sourceDirPath in sourceDirs)
            {
                string relativePath = Path.GetRelativePath(sourcePath, sourceDirPath);
                string destinationDir = Path.Combine(replicaPath, relativePath);
                FileMgr.CreateDir(destinationDir);
            }

            //Copy each source file
            foreach (string sourcefile in sourceFiles)
            {
                string relativePath = Path.GetRelativePath(sourcePath, sourcefile);
                string destinationFile = Path.Combine(replicaPath, relativePath);
                FileMgr.CopyFile(sourcefile, destinationFile);
            }
        }

        private void CleanupReplicaDirectories()
        {
            var replicaDirs = Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories);
            foreach (string replicaDir in replicaDirs)
            {
                string relativePath = Path.GetRelativePath(replicaPath, replicaDir);
                string sourceDir = Path.Combine(sourcePath, relativePath);
                FileMgr.DeleteDir(sourceDir, replicaDir);
            }
        }

        private void CleanupReplicaFiles()
        {
            var replicaFiles = Directory.GetFiles(replicaPath, "*", SearchOption.AllDirectories);
            foreach (string replicafile in replicaFiles)
            {
                string relativePath = Path.GetRelativePath(replicaPath, replicafile);
                string sourceFile = Path.Combine(sourcePath, relativePath);
                FileMgr.DeleteFile(sourceFile, replicafile);
            }
        }
    }
}