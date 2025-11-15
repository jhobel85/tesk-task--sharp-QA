using System;
using System.Timers;
using ReplicaTool.Interfaces;

namespace ReplicaTool.Services
{
    public class Scheduler
    {
        public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

        private readonly System.Timers.Timer timer;

        private readonly IReplicator target;

        public Scheduler(IReplicator target, TimeSpan? interval = null)
        {
            this.target = target;

            var intervalMs = (interval ?? DefaultInterval).TotalMilliseconds;
            timer = new System.Timers.Timer(intervalMs);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
        }

        public void Start() => timer.Start();
        public void Stop() => timer.Stop();

        private void OnTimedEvent(object? sender, ElapsedEventArgs e)
        {
            target.Replicate();
        }

        public void OnExit(object? sender, ConsoleCancelEventArgs e)
        {
            timer.Stop();
            timer.Dispose();
        }

    }
}