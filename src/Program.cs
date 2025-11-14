// See https://aka.ms/new-console-template for more information
Console.WriteLine("App started.");

// Access command-line arguments and print them
foreach (var arg in args)
{
    Console.WriteLine($"Argument: {arg}");
}

ReplicatorOptions options = new ReplicatorOptions();
//TODO: fill options
FolderReplicator replicator = new FolderReplicator(options);
string tmpfilePath = ReplicatorOptions.DefaultReplicaPath + Path.Combine("tmp.txt");
string tmpContent = "XYZ";
replicator.FileMgr.Create(tmpfilePath, tmpContent);

var scheduler = new Scheduler(replicator, options.SyncInterval);
scheduler.Start();
Console.CancelKeyPress += scheduler.OnExit;
Console.WriteLine("Scheduler started. Press Ctrl+C to exit.");

// Block until exit is requested
using var waitHandle = new ManualResetEvent(false);
waitHandle.WaitOne(); 



