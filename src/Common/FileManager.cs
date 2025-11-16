using Serilog;
using ReplicaTool.Interfaces;

namespace ReplicaTool.Common
{
    public class FileManager(string logPath, IFileComparer fileComparer) 
    {
        private readonly ILogger _log = Logger.CreateFileAndConsoleLogger(logPath);
        private readonly IFileComparer _fileComparer = fileComparer;

        public void CreateDir(string destination)
        {
            try
            {
                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                    _log.Information($"Directory created: {destination}");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to create directory: {destination}");
            }
        }

        public void CreateFile(string path, string content)
        {
            try
            {
                // Ensure directory exists
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    CreateDir(directory);
                }

                File.WriteAllText(path, content);
                _log.Information($"File created: {path}");
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to create file: {path}");
            }
        }

        public void CopyFile(string source, string destination)
        {
            try
            {
                DateTime sourceTime = File.GetLastWriteTimeUtc(source);
                DateTime replicaTime = File.GetLastWriteTimeUtc(destination);

                if (!_fileComparer.AreFilesEqual(source, destination))
                {
                    File.Copy(source, destination, true);
                    _log.Information($"File copied: {source} → {destination}");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to copy file: {source} → {destination}");
            }
        }

        public void DeleteDir(string source, string destination)
        {
            try
            {
                if (!Directory.Exists(source))
                {
                    Directory.Delete(destination);
                    _log.Information($"Directory deleted: {destination}");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to delete directory: {destination}");
            }
        }

        public void DeleteFile(string source, string destination)
        {
            try
            {
                if (!File.Exists(source))
                {
                    File.Delete(destination);
                    _log.Information($"File deleted: {destination}");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Failed to delete file: {destination}");
            }
        }

    }
}