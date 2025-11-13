using System.IO;
using Serilog;

public class FileManager
{
    private readonly ILogger _log;

    public FileManager(string logPath)
    {
        _log = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Message}{NewLine}")
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day,
                          outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message}{NewLine}")
            .CreateLogger();
    }

    public void Create(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
            _log.Information($"File created: {path}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to create file: {path}");
        }
    }

    public void Copy(string source, string destination)
    {
        try
        {
            File.Copy(source, destination, true);
            _log.Information($"File copied: {source} → {destination}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to copy file: {source} → {destination}");
        }
    }

        public void Delete(string path)
    {
        try
        {
            File.Delete(path);
            _log.Information($"File deleted: {path}");
        }
        catch (Exception ex)
        {
            _log.Error(ex, $"Failed to delete file: {path}");
        }
    }

}
