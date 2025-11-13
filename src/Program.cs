// See https://aka.ms/new-console-template for more information
Console.WriteLine("App started.");

// Access command-line arguments and print them
foreach (var arg in args)
{
    Console.WriteLine($"Argument: {arg}");
}

FolderReplicator fr = new FolderReplicator();
string tmpfilePath = FolderReplicator.DefaultReplicaPath + Path.Combine("tmp.txt");
string tmpContent = "XYZ";
fr.FileMgr.Create(tmpfilePath, tmpContent);
fr.ReplicateNow();
