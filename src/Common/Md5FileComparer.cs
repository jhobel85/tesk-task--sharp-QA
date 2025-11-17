using System.Security.Cryptography;
using ReplicaTool.Interfaces;
using Serilog;

namespace ReplicaTool.Common
{
    public class Md5FileComparer : IFileComparer
    {
        private readonly ILogger _log = Logger.CLI_LOGGER;

        /**
         Check computed MD5 has for better performance especially when 
         copmaring large files or high valueme of small files. 
        */
        public bool AreFilesEqual(string sourcePath, string replicaPath)
        {
            bool ret = false;
            try
            {
                if (File.Exists(replicaPath))
                {
                    using var md5 = MD5.Create();
                    using var srcStream = File.OpenRead(sourcePath);
                    using var destStream = File.OpenRead(replicaPath);

                    var srcHash = md5.ComputeHash(srcStream);
                    var destHash = md5.ComputeHash(destStream);

                    // If lengths differ -> safe time by NOT comparing
                    if (srcHash.Length == destHash.Length)
                    {
                        ret = srcHash.SequenceEqual(destHash);
                    }                                        
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
            _log.Debug($"Md5FileComparer.AreFilesEqual {ret}: {sourcePath} <-> {replicaPath}");

            return ret;
        }
    }
}