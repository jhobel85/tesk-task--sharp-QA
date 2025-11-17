using System.Threading;
using System.Threading.Tasks;

namespace ReplicaTool.Interfaces
{
    public interface IFileManager
    {
        void CreateDir(string? path);
        void CreateFile(string path, string content);
        Task CopyFileAsync(string source, string destination, CancellationToken cancellationToken = default);
        void DeleteDir(string path, bool recursive = true);
        void DeleteFile(string path);
    }
}
