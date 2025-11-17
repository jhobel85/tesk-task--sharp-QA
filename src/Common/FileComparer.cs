using ReplicaTool.Interfaces;
using Serilog;

namespace ReplicaTool.Common
{
    public class FileComparer : IFileComparer
    {
        private readonly ILogger _log = Logger.CLI_LOGGER;

        /**
         Basic checks according to file size and timestamp of files.
        */
        public bool AreFilesEqual(string sourcePath, string replicaPath)
        {
            bool ret = false;
            try
            {
                if (File.Exists(replicaPath))
                {
                    var srcFile = new FileInfo(sourcePath);
                    var destFile = new FileInfo(replicaPath);
                    bool lengthEqual = srcFile.Length == destFile.Length;
                    bool timeEqual = srcFile.LastWriteTimeUtc != destFile.LastWriteTimeUtc;
                    ret = lengthEqual && timeEqual;
                }
                else
                {
                    _log.Debug($"Destination file does not exist: {replicaPath}");
                }
            }
            catch (FileNotFoundException ex)
            {
                _log.Error(ex, $"File {sourcePath} not found during comparison");
                ret = false;
            }
            catch (IOException ex)
            {
                _log.Error(ex, $"I/O error during file comparison: {sourcePath} and {replicaPath}");
                ret = false;
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unexpected error during file comparison: {sourcePath} and {replicaPath}");
                ret = false;
            }

            _log.Debug($"FileComparer.AreFilesEqual {ret}: {sourcePath} <-> {replicaPath}");

            return ret;
        }
    }
}