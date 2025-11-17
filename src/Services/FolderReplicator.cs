using ReplicaTool.Interfaces;

namespace ReplicaTool.Services
{
    public class FolderReplicator(IReplicatorOptions options, IFileManager fileMgr) : IReplicator
    {
        public IFileManager FileMgr { get; private set; } = fileMgr;
        private readonly string _sourcePath = options.SourcePath;
        private readonly string _replicaPath = options.ReplicaPath;

        public async Task ReplicateAsync(CancellationToken cancellationToken = default)
        {
            EnsurePathsExist(); // create source and replica paths if not exist
            await SyncPathsAsync(cancellationToken).ConfigureAwait(false); // Copy new directories and files (bounded concurrency)
            CleanupReplicaFiles(cancellationToken); // delete files not present in source
            CleanupReplicaDirectories(cancellationToken); // delete directories not present in source            
        }

        private void EnsurePathsExist()
        {
            FileMgr.CreateDir(_sourcePath);
            FileMgr.CreateDir(_replicaPath);
        }

        private async Task SyncPathsAsync(CancellationToken cancellationToken = default)
        {
            // Get list of sub-directories in source and create them in replica
            var sourceDirs = Directory.GetDirectories(_sourcePath, "*", SearchOption.AllDirectories);
            foreach (string sourceDirPath in sourceDirs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = Path.GetRelativePath(_sourcePath, sourceDirPath);
                string destinationDir = Path.Combine(_replicaPath, relativePath);
                FileMgr.CreateDir(destinationDir);
            }

            // Get the list of files in source directory and copy them to replica using bounded concurrency
            var sourceFiles = Directory.GetFiles(_sourcePath, "*", SearchOption.AllDirectories);
            var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };

            var tasks = new List<Task>(sourceFiles.Length);
            await Parallel.ForEachAsync(sourceFiles, parallelOptions, async (sourcefile, ct) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = Path.GetRelativePath(_sourcePath, sourcefile);
                string destinationFile = Path.Combine(_replicaPath, relativePath);
                await FileMgr.CopyFileAsync(sourcefile, destinationFile, ct).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private void CleanupReplicaDirectories(CancellationToken cancellationToken = default)
        {
            var replicaDirs = Directory.GetDirectories(_replicaPath, "*", SearchOption.AllDirectories);
            foreach (string replicaDir in replicaDirs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = Path.GetRelativePath(_replicaPath, replicaDir);
                string sourceDir = Path.Combine(_sourcePath, relativePath);
                //Cleanup only if source directory does not exist
                if (!Directory.Exists(sourceDir))
                {
                    FileMgr.DeleteDir(replicaDir);
                }
            }
        }

        private void CleanupReplicaFiles(CancellationToken cancellationToken = default)
        {
            var replicaFiles = Directory.GetFiles(_replicaPath, "*", SearchOption.AllDirectories);
            foreach (string replicafile in replicaFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string relativePath = Path.GetRelativePath(_replicaPath, replicafile);
                string sourceFile = Path.Combine(_sourcePath, relativePath);
                //Cleanup only if source file does not exist
                if (!File.Exists(sourceFile))
                {
                    FileMgr.DeleteFile(replicafile);
                }
            }
        }
    }
}