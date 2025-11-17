using System.Timers;
using ReplicaTool.Interfaces;
using ReplicaTool.Common;
using Serilog;

namespace ReplicaTool.Services
{
    public class Scheduler
    {
        private readonly ILogger _log = Logger.CLI_LOGGER;

        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

        private readonly System.Timers.Timer _timer;

        private readonly IReplicator _target;
        private CancellationTokenSource? _cts;

        public Scheduler(IReplicator target, TimeSpan? interval = null)
        {
            _target = target;

            var intervalMs = (interval ?? DefaultInterval).TotalMilliseconds;
            _timer = new System.Timers.Timer(intervalMs);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _cts?.Cancel();
        }

        private async void OnTimedEvent(object? sender, ElapsedEventArgs e)
        {
            try
            {
                await _target.ReplicateAsync(_cts?.Token ?? default).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _log.Information("Replication canceled.");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unexpected error occured during replication.");
            }
        }

        public void OnExit(object? sender, ConsoleCancelEventArgs e)
        {
            _timer.Stop();
            _timer.Dispose();
            _cts?.Cancel();
        }

    }
}