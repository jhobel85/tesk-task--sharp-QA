using ReplicaTool.Interfaces;
using Serilog;

namespace ReplicaTool.Common
{
    /**
        Adaptive file comparer facade (ByteByByteComparer vs CacheHashFileComparer) with configurable buffer sizes for small and large files.

        Strategy:
        - Small files (<= smallThreshold): direct byte comparison using smaller buffer to minimize latency.
        - Large files (> smallThreshold): cached hash comparison using larger buffer for throughput & reuse.

        Constructor parameters:
        - smallBufferSize (default 32KB)
        - largeBufferSize (default 128KB; coerced to >= smallBufferSize)
        - smallThresholdBytes (default 256KB)

    */
    public class AdaptiveFileComparer : IFileComparer
    {
        private readonly ILogger _log = Logger.CLI_LOGGER;
        private readonly ByteByByteComparer _direct;
        private readonly CacheHashFileComparer _cached;
        private readonly long _smallThreshold;

        public AdaptiveFileComparer(int smallBufferSize = 32 * 1024, int largeBufferSize = 128 * 1024, long smallThresholdBytes = 256 * 1024)
        {
            if (largeBufferSize < smallBufferSize)
                largeBufferSize = smallBufferSize;
            _direct = new ByteByByteComparer(smallBufferSize);
            _cached = new CacheHashFileComparer(largeBufferSize);
            _smallThreshold = smallThresholdBytes;
        }

        public bool AreFilesEqual(string sourcePath, string replicaPath)
        {
            try
            {
                var srcInfo = new FileInfo(sourcePath);
                var replicaInfo = new FileInfo(replicaPath);

                if (!srcInfo.Exists)
                {                    
                    return false; // Source file missing
                }

                if (!replicaInfo.Exists)
                {                                        
                    return false; //Replica file missing
                }

                if (srcInfo.Length != replicaInfo.Length)
                {
                    return false; // size mismatch fast path
                }

                if (srcInfo.Length == 0)
                {
                    return true; // both empty
                }

                bool ret = false;
                long size = srcInfo.Length;
                if (size <= _smallThreshold)
                {
                    // Use direct byte comparison for small files to avoid hashing overhead
                    ret = _direct.AreFilesEqual(sourcePath, replicaPath);
                }
                else
                {
                    // Use cached hash comparison for larger files to benefit from reuse
                    ret = _cached.AreFilesEqual(sourcePath, replicaPath);
                }

                _log.Debug($"AdaptiveFileComparer.AreFilesEqual {ret}: {sourcePath} <-> {replicaPath} (size={size} bytes)");
                return ret;
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Adaptive comparison failed: {sourcePath} <-> {replicaPath}");
                return false;
            }
        }
    }
}
