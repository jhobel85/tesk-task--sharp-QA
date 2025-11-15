using Serilog;

namespace ReplicaTool.Common
{
    public class Logger
    {
        public static ILogger CreateConsoleLogger()
        {
            return new LoggerConfiguration()
                .WriteTo.Console(outputTemplate: "[{Message}{NewLine}")
                .CreateLogger();
        }
        
        public static ILogger CreateFileAndConsoleLogger(string logPath) =>         
             new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Message}{NewLine}")
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Message}{NewLine}")
            .CreateLogger();
    };
}