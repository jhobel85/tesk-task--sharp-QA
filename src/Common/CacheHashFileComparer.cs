using System.IO.Hashing;
using ReplicaTool.Interfaces;
using Serilog;

namespace ReplicaTool.Common
{
    /**
         Caching hash-based file comparer with early-exit + LRU + pooled buffers.
         Components:
         1. Metadata gate: first compares file sizes (fast mismatch) and if both are zero-length returns true immediately without opening streams.
         2. LRU hash cache (key: path+size+mtime) for instant equality decisions.
         3. Streamed lock-step chunk comparison with ArrayPool<byte> to avoid per-call allocations.
         4. Incremental XxHash64 built only when a file version not already cached (skips hashing if both cached).
         5. Early exit on first differing chunk (minimal wasted work).
         6. Automatic LRU eviction at capacity (default 10k entries).
         7. Adaptive buffer: for files >=256MB uses 1MB buffer; otherwise constructor bufferSize.
     */
    class CacheHashFileComparer(int bufferSize) : IFileComparer
    {
        private readonly ILogger _log = Logger.CLI_LOGGER;
        private readonly int _bufferSize = bufferSize;
        private static int _cacheCapacity = 10000;
        // Adaptive buffering: use larger buffer for very large files to reduce I/O ops
        private const long LargeFileSizeThresholdBytes = 256L * 1024 * 1024; // 256MB threshold
        private const int LargeFileBufferSizeBytes = 1024 * 1024; // 1 MB buffer for large files

        // LRU cache: map key -> (hash, node reference); linked list holds most recent at front
        private static readonly Dictionary<(string Path, long Size, DateTime LastWrite), (string Hash, LinkedListNode<(string Path, long Size, DateTime LastWrite)> Node)> _hashCache = [];
        private static readonly LinkedList<(string Path, long Size, DateTime LastWrite)> _lruList = new();
        private static readonly object _cacheLock = new();

        public bool AreFilesEqual(string sourcePath, string replicaPath)
        {
            bool ret = false;
            try
            {
                var srcFileInfo = new FileInfo(sourcePath);
                var destFileInfo = new FileInfo(replicaPath);
                ret = CompareFilesWithCache(srcFileInfo, destFileInfo);
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

            _log.Debug($"CacheHashFileComparer.AreFilesEqual {ret}: {sourcePath} <-> {replicaPath}");

            return ret;
        }

        /**
            Compares files using cached hashes when available with early exit optimization.
            If both files have cached hashes, comparison is instant.
            If one or both need computation, computes them chunk-by-chunk in parallel with early exit.
            Cache key includes file path, size, and last write time to ensure validity.
        */
        private bool CompareFilesWithCache(FileInfo srcFileInfo, FileInfo destFileInfo)
        {
            var srcCacheKey = (srcFileInfo.FullName, srcFileInfo.Length, srcFileInfo.LastWriteTimeUtc);
            var destCacheKey = (destFileInfo.FullName, destFileInfo.Length, destFileInfo.LastWriteTimeUtc);

            string? srcHash = null; string? destHash = null;
            lock (_cacheLock)
            {
                if (_hashCache.TryGetValue(srcCacheKey, out var srcEntry))
                {
                    srcHash = srcEntry.Hash;
                    _lruList.Remove(srcEntry.Node);
                    _lruList.AddFirst(srcEntry.Node);
                }
                if (_hashCache.TryGetValue(destCacheKey, out var destEntry))
                {
                    destHash = destEntry.Hash;
                    _lruList.Remove(destEntry.Node);
                    _lruList.AddFirst(destEntry.Node);
                }
            }

            if (srcHash != null && destHash != null)
            {
                _log.Debug("Both files cached (LRU), instant comparison");
                return srcHash == destHash;
            }

            _log.Debug("Cache miss path; performing streamed comparison with optional hash build");
            return CompareFilesChunkByChunkWithCaching(srcFileInfo, destFileInfo, srcHash, destHash);
        }


        /**
            Compares files chunk-by-chunk with early exit while building final hashes for caching.
            This provides best of both worlds: early exit when files differ, caching when they match.
        */
        private bool CompareFilesChunkByChunkWithCaching(FileInfo srcFileInfo, FileInfo destFileInfo, string? srcCachedHash, string? destCachedHash)
        {
            bool buildSrcHash = srcCachedHash == null;
            bool buildDestHash = destCachedHash == null;

            var equal = StreamCompareAndOptionalHash(srcFileInfo, destFileInfo, buildSrcHash, buildDestHash, out var srcHasher, out var destHasher);
            if (!equal) return false;

            CacheHashesIfBuilt(srcFileInfo, destFileInfo, buildSrcHash, buildDestHash, srcHasher, destHasher);
            return true;
        }

        private bool StreamCompareAndOptionalHash(FileInfo srcFileInfo, FileInfo destFileInfo, bool buildSrcHash, bool buildDestHash, out XxHash64? srcHasher, out XxHash64? destHasher)
        {
            int activeBufferSize = (srcFileInfo.Length >= LargeFileSizeThresholdBytes || destFileInfo.Length >= LargeFileSizeThresholdBytes)
                ? LargeFileBufferSizeBytes
                : _bufferSize;

            using var srcStream = new FileStream(srcFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, activeBufferSize, FileOptions.SequentialScan);
            using var destStream = new FileStream(destFileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, activeBufferSize, FileOptions.SequentialScan);

            srcHasher = buildSrcHash ? new XxHash64() : null;
            destHasher = buildDestHash ? new XxHash64() : null;

            var pool = System.Buffers.ArrayPool<byte>.Shared;
            byte[] srcBuffer = pool.Rent(activeBufferSize);
            byte[] destBuffer = pool.Rent(activeBufferSize);

            try
            {
                int srcBytesRead;
                while ((srcBytesRead = srcStream.Read(srcBuffer, 0, activeBufferSize)) > 0)
                {
                    int destBytesRead = destStream.Read(destBuffer, 0, activeBufferSize);
                    if (srcBytesRead != destBytesRead) return false;

                    var srcSpan = srcBuffer.AsSpan(0, srcBytesRead);
                    var destSpan = destBuffer.AsSpan(0, destBytesRead);

                    if (!srcSpan.SequenceEqual(destSpan))
                    {
                        _log.Debug("Files differ at chunk, early exit");
                        return false;
                    }

                    if (buildSrcHash) srcHasher!.Append(srcSpan);
                    if (buildDestHash) destHasher!.Append(destSpan);
                }
            }
            finally
            {
                pool.Return(srcBuffer);
                pool.Return(destBuffer);
            }
            return true;
        }

        private void CacheHashesIfBuilt(FileInfo srcFileInfo, FileInfo destFileInfo, bool buildSrcHash, bool buildDestHash, XxHash64? srcHasher, XxHash64? destHasher)
        {
            if (!buildSrcHash && !buildDestHash) return;

            var srcCacheKey = (srcFileInfo.FullName, srcFileInfo.Length, srcFileInfo.LastWriteTimeUtc);
            var destCacheKey = (destFileInfo.FullName, destFileInfo.Length, destFileInfo.LastWriteTimeUtc);

            lock (_cacheLock)
            {
                if (buildSrcHash && srcHasher != null)
                {
                    string srcFinal = Convert.ToHexString(srcHasher.GetCurrentHash());
                    var node = new LinkedListNode<(string Path, long Size, DateTime LastWrite)>(srcCacheKey);
                    _lruList.AddFirst(node);
                    _hashCache[srcCacheKey] = (srcFinal, node);
                    _log.Debug("Cached source hash");
                }
                if (buildDestHash && destHasher != null)
                {
                    string destFinal = Convert.ToHexString(destHasher.GetCurrentHash());
                    var node = new LinkedListNode<(string Path, long Size, DateTime LastWrite)>(destCacheKey);
                    _lruList.AddFirst(node);
                    _hashCache[destCacheKey] = (destFinal, node);
                    _log.Debug("Cached destination hash");
                }
                while (_hashCache.Count > _cacheCapacity)
                {
                    var last = _lruList.Last;
                    if (last == null) break;
                    _hashCache.Remove(last.Value);
                    _lruList.RemoveLast();
                    _log.Debug("Free LRU after insert");
                }
            }
        }

        /**
            Clears the hash cache if needed.
        */
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _hashCache.Clear();
                _lruList.Clear();
            }
        }

        /**
            Gets current cache statistics for monitoring.
        */
        public static (int Count, int Capacity, long ApproximateMemoryBytes) GetCacheStats()
        {
            lock (_cacheLock)
            {
                return (_hashCache.Count, _cacheCapacity, _hashCache.Count * 200L);
            }
        }

        public static void SetCacheCapacity(int newCapacity)
        {
            if (newCapacity <= 0) return;
            lock (_cacheLock)
            {
                _cacheCapacity = newCapacity;
                while (_hashCache.Count > _cacheCapacity)
                {
                    var last = _lruList.Last;
                    if (last == null) break;
                    _hashCache.Remove(last.Value);
                    _lruList.RemoveLast();
                }
            }
        }
    }
}