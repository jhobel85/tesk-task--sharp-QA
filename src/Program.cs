// See https://aka.ms/new-console-template for more information
Console.WriteLine("App started.");

//ReplicatorOptions options = new ReplicatorOptions();
IReplicatorOptions options = ReplicatorOptionsCmd.Parse(args);

if (!options.ArgumentsProvided())
{
    Console.WriteLine("App exited.");
    return;
}

FolderReplicator replicator = new FolderReplicator(options);
string tmpfilePath = options.ReplicaPath + Path.Combine("tmp.txt");
string tmpContent = "XYZ";
replicator.FileMgr.Create(tmpfilePath, tmpContent);

var scheduler = new Scheduler(replicator, options.SyncInterval);
scheduler.Start();
Console.CancelKeyPress += scheduler.OnExit;
Console.WriteLine("Scheduler started. Press Ctrl+C to exit.");

// Block until exit is requested
using var waitHandle = new ManualResetEvent(false);
waitHandle.WaitOne(); 



