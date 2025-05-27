using System.Text;

namespace DefaultNamespace;

public class Logger
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

    private readonly string _logDirectory;
    private readonly string _sessionFolder;
    private Dictionary<string, StreamWriter> _logWriters;

    public Logger()
    {
        // Create logs in the application directory
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BPtoPNLogs");
        _sessionFolder = Path.Combine(_logDirectory, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        _logWriters = new Dictionary<string, StreamWriter>();

        InitializeLogDirectory();
    }

    private void InitializeLogDirectory()
    {
        // Create main logs directory if it doesn't exist
        Directory.CreateDirectory(_logDirectory);
        // Create session-specific directory
        Directory.CreateDirectory(_sessionFolder);

        // Initialize different log files
        CreateLogWriter("general");
        CreateLogWriter("processing");
        CreateLogWriter("errors");
    }

    private void CreateLogWriter(string logType)
    {
        var logPath = Path.Combine(_sessionFolder, $"{logType}.log");
        var writer = new StreamWriter(logPath, true, Encoding.UTF8) {AutoFlush = true};
        _logWriters[logType] = writer;
    }

    public void Log(string message, LogLevel level = LogLevel.Info, string logType = "general")
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] [{level}] {message}";

        /*      // Write to console with color coding
              var originalColor = Console.ForegroundColor;
              Console.ForegroundColor = GetColorForLogLevel(level);
              Console.WriteLine(logMessage);
              Console.ForegroundColor = originalColor;
      */
        // Write to file
        if (_logWriters.ContainsKey(logType))
        {
            _logWriters[logType].WriteLine(logMessage);
        }
    }

    private ConsoleColor GetColorForLogLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
    }

    public void LogProcessingInfo(string message)
    {
        Log(message, LogLevel.Info, "processing");
    }

    public void LogError(string message, Exception ex)
    {
        var errorMessage = $"{message}\nException: {ex.Message}\nStack Trace: {ex.StackTrace}";
        Log(errorMessage, LogLevel.Error, "errors");
    }

    public void Dispose()
    {
        foreach (var writer in _logWriters.Values)
        {
            writer.Flush();
            writer.Dispose();
        }
    }
}