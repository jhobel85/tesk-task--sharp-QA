using System.Security.Cryptography;
using ReplicaTool.Interfaces;
using Serilog;

namespace ReplicaTool.Common
{

/**
 Check computed MD5 has for better performance especially when 
 copmaring large files or high valueme of small files. 
*/
    public class Md5FileComparer : IFileComparer
    {
        private readonly ILogger _log = Logger.CLI_LOGGER;

        public bool AreFilesEqual(string sourcePath, string replicaPath)
        {
            bool ret = false;
            try
            {
                if (File.Exists(replicaPath))
                {
                    var srcFileInfo = new FileInfo(sourcePath);
                    var destFileInfo = new FileInfo(replicaPath);
                    if (srcFileInfo.Length != destFileInfo.Length)
                    {
                        // Length differ -> files differ, avoid computing hash
                        return false;
                    }

                    byte[] srcHash = ComputeHash(sourcePath);
                    byte[] destHash = ComputeHash(replicaPath);
                    ret = srcHash.SequenceEqual(destHash);
                }
                else
                {
                    _log.Debug($"ReplicaPath file does not exist: {replicaPath}");
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
            _log.Debug($"Md5FileComparer.AreFilesEqual {ret}: {sourcePath} <-> {replicaPath}");

            return ret;
        }

        /**
            Compute MD5 hashe by streaming to avoid high memory consumption.
            Each file need to have it own MD5 instance.
        */
        private byte[] ComputeHash(string filePath)
        {
            byte[] ret;
            using (var md5Src = MD5.Create())
            using (var srcStream = File.OpenRead(filePath))
            {
                ret = md5Src.ComputeHash(srcStream);
            }
            return ret;
        }
    }
}