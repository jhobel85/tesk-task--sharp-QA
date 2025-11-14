using System;
using System.Timers;

public class Scheduler
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);

    private readonly System.Timers.Timer timer;

    private readonly IScheduler target;

    public Scheduler(IScheduler target, TimeSpan? interval = null)
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
        target.Update();
    }

     public void OnExit(object? sender, ConsoleCancelEventArgs e)
    {
        timer.Stop();
        timer.Dispose();
    }

}
