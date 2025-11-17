using Xunit;
using Moq;
using ReplicaTool.Configuration;
using ReplicaTool.Interfaces;
using ReplicaTool.Services;

namespace ReplicaTool
{
    public class ReplicatorCancellationTest
    {
        [Fact]
        public async Task CancellationWhileReplicateAsyncMoq()
        {       
            // setup temp directories for source and replica
            var root_tmp = Path.Combine(Path.GetTempPath(), "ReplicaTool.Tests", Guid.NewGuid().ToString());
            var source = Path.Combine(root_tmp, "source");
            var replica = Path.Combine(root_tmp, "replica");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(replica);
            
            try
            {                
                // create a small tmp file to ensure FolderReplicator has something to copy
                File.WriteAllText(Path.Combine(source, "tmp.txt"), "tempFile content");

                // create a mock of IFileManager
                var mockMgr = new Mock<IFileManager>();

                // Setup CopyFileAsync to return a Task that finished when CancellationToken is cancelled
                mockMgr
                    .Setup(m => m.CopyFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns<string, string, CancellationToken>((s, d, ct) => Task.Delay(Timeout.Infinite, ct));

                var options = new ReplicatorOptions
                {
                    SourcePath = source,
                    ReplicaPath = replica,
                    LogFilePath = Path.Combine(root_tmp, "test.log"),                    
                    SyncInterval = TimeSpan.FromSeconds(5),
                };

                var replicator = new FolderReplicator(options, mockMgr.Object);

                using var cts = new CancellationTokenSource();
                var replicateTask = replicator.ReplicateAsync(cts.Token);

                // let the replicate start and run the mocked long copy operation
                await Task.Delay(100);

                // cancel the Task returned by CopyFileAsync
                cts.Cancel();

                // await the replicate task and verify cancellation occured
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => replicateTask);
            }
            finally
            {
                try { Directory.Delete(root_tmp, true); } catch { }
            }
        }
    }
}
