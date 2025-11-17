using ReplicaTool.Common;
using ReplicaTool.Configuration;
using ReplicaTool.Services;

var _log = Logger.CLI_LOGGER;
_log.Information("App started.");


var options = ReplicatorOptionsCmd.Parse(args);

if (!options.ArgumentsProvided())
{
    _log.Error("App exited due to invalid arguments.");
    return;
}

var comparer = new Md5FileComparer();
var fileMgr = new FileManager(options.LogFilePath, comparer);
var replicator = new FolderReplicator(options, fileMgr);

var scheduler = new Scheduler(replicator, options.SyncInterval);
scheduler.Start();
Console.CancelKeyPress += scheduler.OnExit;
_log.Information("Scheduler started. Press Ctrl+C to exit.");

// Block until exit is requested
using var waitHandle = new ManualResetEvent(false);
waitHandle.WaitOne(); 



