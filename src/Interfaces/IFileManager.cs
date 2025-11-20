using System.Threading;
using System.Threading.Tasks;

namespace ReplicaTool.Interfaces
{
    public interface IFileManager
    {
        void CreateDirIfNotExists(string? path);
        void CreateFileIfNotExists(string path, string content);
        Task CopyFileAsync(string source, string destination, CancellationToken cancellationToken = default);
        void DeleteDir(string path, bool recursive = true);
        void DeleteFile(string path);
    }
}
