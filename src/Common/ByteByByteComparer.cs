using System.Security.Cryptography;
using ReplicaTool.Interfaces;
using Serilog;

namespace ReplicaTool.Common
{
    /**
        High-performance direct (non-hash) file comparer.

        What it does:
        - Opens both files as forward-only FileStreams (SequentialScan hint)
        - Reads aligned chunks (bufferSize) from each
        - Compares spans with Span<byte>.SequenceEqual (SIMD optimized) per chunk
        - Exits immediately on first mismatch (no full-file read if different early)
        - Detects size differences implicitly when read lengths diverge

        When to use:
        - Small or transient files where hashing overhead is wasteful
        - Scenarios where most files differ and early exit saves I/O

        When NOT to use (prefer cached hashing strategy):
        - Large files repeatedly compared across sync cycles (CacheHashFileComparer / XxHash64Comparer gains reuse)

        Performance notes:
        - bufferSize default should generally be 64–128KB for spinning disks, 32–256KB for SSD; caller supplies value.
        - No allocations besides two buffers.
        - No hashing cost - CPU usage proportional to differing bytes examined.
     */
    class ByteByByteComparer(int bufferSize) : IFileComparer
    {
        private readonly ILogger _log = Logger.CLI_LOGGER;
        private readonly int _bufferSize = bufferSize;

        public bool AreFilesEqual(string sourcePath, string replicaPath)
        {
            bool ret = false;
            try
            {
                // Compare files byte-by-byte with early exit on first difference
                ret = CompareFileContents(sourcePath, replicaPath);
            }
            catch (FileNotFoundException ex)
            {
                _log.Error(ex, $"File {sourcePath} or {replicaPath} not found during comparison");                
            }
            catch (IOException ex)
            {
                _log.Error(ex, $"I/O error during file comparison: {sourcePath} and {replicaPath}");                
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Unexpected error during file comparison: {sourcePath} and {replicaPath}");                
            }
            _log.Debug($"ByteByByteComparer.AreFilesEqual {ret}: {sourcePath} <-> {replicaPath}");

            return ret;
        }

        /**
            Compare file contents byte by byte using buffered streams.
            Exits immediately on first difference found.
        */
        private bool CompareFileContents(string sourcePath, string replicaPath)
        {
            using var srcStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, FileOptions.SequentialScan);
            using var destStream = new FileStream(replicaPath, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, FileOptions.SequentialScan);

            byte[] srcBuffer = new byte[_bufferSize];
            byte[] destBuffer = new byte[_bufferSize];

            int srcBytesRead, destBytesRead;
            bool filesAreEqual = true;
            while (filesAreEqual && (srcBytesRead = srcStream.Read(srcBuffer, 0, _bufferSize)) > 0)
            {
                destBytesRead = destStream.Read(destBuffer, 0, _bufferSize);

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