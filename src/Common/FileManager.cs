using Serilog;
using ReplicaTool.Interfaces;

namespace ReplicaTool.Common
{
    public class FileManager(string logPath, IFileComparer fileComparer) : IFileManager
    {
        private readonly ILogger _log = Logger.CreateFileAndConsoleLogger(logPath);
        private readonly IFileComparer _fileComparer = fileComparer;
        public const int BufferSize = 81920;
        public void CreateDir(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _log.Warning("CreateDir called with null or empty destination.");
                return;
            }

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    _log.Information($"Directory created: {path}");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to create directory: {path}");
            }
        }

        public void CreateFile(string path, string content)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _log.Warning("CreateFile called with null or empty path.");
                return;
            }

            try
            {
                string? directory = Path.GetDirectoryName(path);
                CreateDir(directory); // Ensure directory exists

                if (!File.Exists(path))
                {
                    File.WriteAllText(path, content);
                    _log.Information($"File created: {path}");
                }
                else
                {
                    _log.Information($"File already exists, skipping creation: {path}");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to create file: {path}");
            }
        }

        public virtual async Task CopyFileAsync(string source, string destination, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
            {
                _log.Warning($"CopyFileAsync called with null or empty path(s): source='{source}', destination='{destination}'");
                return;
            }

            try
            {
                if (!File.Exists(source))
                {
                    _log.Warning($"Source file does not exist: {source}");
                    return;
                }

                string? destDir = Path.GetDirectoryName(destination);
                CreateDir(destDir); // Ensure directory exists

                if (!_fileComparer.AreFilesEqual(source, destination))
                {
                    // perform an async copy with enabled cancellation
                    using (var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, useAsync: true))
                    using (var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, useAsync: true))
                    {
                        await sourceStream.CopyToAsync(destStream, BufferSize, cancellationToken).ConfigureAwait(false);
                    }

                    _log.Information($"File copied: {source} → {destination}");
                }
            }
            catch (OperationCanceledException)
            {
                _log.Warning($"CopyToAsync canceled: {source} → {destination}");
                throw;
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to copy file: {source} → {destination}");
            }
        }

        public void DeleteDir(string path, bool recursive = true)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _log.Warning("DeleteDir called with null or empty destination.");
                return;
            }

            try
            {
                if (!Directory.Exists(path))
                {

                    Directory.Delete(path, recursive); // delete also non-empty directories 
                    _log.Warning($"Directory deleted: {path}");
                }
                else
                {
                    _log.Debug($"Skipped deletion: Directory NOT exists '{path}'");
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to delete directory: {path}");
            }
        }

        public void DeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                _log.Warning("DeleteFile called with null or empty destination.");
                return;
            }

            try
            {
                if (!File.Exists(path))
                {

                    File.Delete(path);
                    _log.Warning($"File deleted: {path}");
                }
                else
                {
                    _log.Debug($"Skipped deletion: File NOT exists '{path}'");
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to delete file: {path}");
            }
        }

    }
}