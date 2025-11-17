using System.Threading;
using System.Threading.Tasks;

namespace ReplicaTool.Interfaces
{
    public interface IReplicator
    {
        Task ReplicateAsync(CancellationToken cancellationToken = default);
    }
}