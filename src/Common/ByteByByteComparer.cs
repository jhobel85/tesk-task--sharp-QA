using System.Security.Cryptography;
using ReplicaTool.Interfaces;
using Serilog;

namespace ReplicaTool.Common
{
    /**
         Efficiently compares local files by reading and comparing on the fly.
         Exits as soon as a difference is found, which it much faster
         than reading entire files and computing MD5 hashes from it.
     */
    public class ByteByByteComparer : IFileComparer
    {
        private readonly ILogger _log = Logger.CLI_LOGGER;
        private const int BufferSize = 81920; // 80KB buffer for optimal I/O performance

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
                        // Length differ -> files differ, avoid expensive content comparison         
                        return false;
                    }

                    if (srcFileInfo.Length == 0)
                    {
                        //Both files are empty (lenght 0) -> they are equal, avoid content comparison
                        return true;
                    }

                    // Compare files byte-by-byte with early exit on first difference
                    ret = CompareFileContents(sourcePath, replicaPath);
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

        /**
            Compare file contents byte by byte using buffered streams.
            Exits immediately on first difference found.
        */
        private bool CompareFileContents(string sourcePath, string replicaPath)
        {
            using var srcStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
            using var destStream = new FileStream(replicaPath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan);
            
            byte[] srcBuffer = new byte[BufferSize];
            byte[] destBuffer = new byte[BufferSize];
            
            int srcBytesRead, destBytesRead;
            bool filesAreEqual = true;
            while (filesAreEqual && (srcBytesRead = srcStream.Read(srcBuffer, 0, BufferSize)) > 0)
            {
                destBytesRead = destStream.Read(destBuffer, 0, BufferSize);
                
                if (srcBytesRead != destBytesRead)
                {
                    //Prevent race condition if file sizes changed during read!
                    filesAreEqual = false;
                }
                
                // Fast comparison using Span with early exit on first different buffer
                var srcSpan = new ReadOnlySpan<byte>(srcBuffer, 0, srcBytesRead);
                var destSpan = new ReadOnlySpan<byte>(destBuffer, 0, destBytesRead);
                if (!srcSpan.SequenceEqual(destSpan))
                {
                    filesAreEqual = false;
                }
            }
            
            return filesAreEqual;
        }
    }
}