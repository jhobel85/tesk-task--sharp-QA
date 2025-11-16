using System.Security.Cryptography;
using System.Collections;
using ReplicaTool.Interfaces;
using Serilog;
using System.Data.Common;

namespace ReplicaTool.Common
{
    public class Md5FileComparer : IFileComparer
    {
        private readonly ILogger _log = Logger.CLI_LOGGER;
        
        /**
         Checkl file size and timestamp before computing MD5 for better performance when 
         copmaring large files or high valueme of small files. 
        */
        public bool AreFilesEqual(string sourcePath, string replicaPath)
        {            
            try
            {
                if (!File.Exists(replicaPath))
                    return false; // do not other checks when replica file does not exists.

                var srcFile = new FileInfo(sourcePath);
                var destFile = new FileInfo(replicaPath);
                if (srcFile.Length != destFile.Length)
                    return false;

/*
//Comment out to rely only on MD5 hash comparison
                if (srcFile.LastWriteTimeUtc != destFile.LastWriteTimeUtc)
                    return false;
*/
                using var md5 = MD5.Create();
                using var srcStream = File.OpenRead(sourcePath);
                using var destStram = File.OpenRead(replicaPath);

                var srcHash = md5.ComputeHash(srcStream);
                var destHash = md5.ComputeHash(destStram);

                return StructuralComparisons.StructuralEqualityComparer.Equals(srcHash, destHash);
            }
            catch (FileNotFoundException ex)
            {
                _log.Error(ex, $"File {sourcePath} not found during comparison");
                return false;
            }
            catch (IOException ex)
            {
                _log.Error(ex, $"I/O error during file comparison: {sourcePath} and {replicaPath}");
                return false;
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unexpected error during file comparison: {sourcePath} and {replicaPath}");
                return false;
            }
        }
    }
}