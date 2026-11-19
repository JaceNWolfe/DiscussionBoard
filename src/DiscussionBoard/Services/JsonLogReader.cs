using System.Text.Json;
using DiscussionBoard.Models;

namespace DiscussionBoard.Services
{
    /// <summary>
    ///
    /// </summary>
    public class JsonLogReader : ILogReader
    {
        private readonly string _logDirectory;
        private readonly ILogger<JsonLogReader> _logger;

        /// <summary>
        ///
        /// </summary>
        /// <param name="environment"></param>
        public JsonLogReader(IWebHostEnvironment environment, ILogger<JsonLogReader> logger)
        {
            _logDirectory = Path.Combine(environment.ContentRootPath, "Logs");
            _logger = logger;
        }

        public IEnumerable<LogEntry> Read()
        {
            string[] logFiles;
            List<LogEntry> logs = new List<LogEntry>();

            try
            {
                logFiles = Directory.GetFiles(_logDirectory, "*.json");
            }
            catch (DirectoryNotFoundException)
            {
                return logs;
            }
            catch (IOException e)
            {
                _logger.LogError(
                    e,
                    "Could not read log files from {LogDirectory}",
                    _logDirectory);
                throw;
            }

            foreach (string logFile in logFiles)
            {
                FileStream? stream = null;

                stream = GetFileStream(logFile);

                using StreamReader reader = new(stream);

                try
                {
                    LoadLogs(reader, logFile, logs);
                }
                catch (IOException e)
                {
                    _logger.LogError(
                        e,
                        "Could not read log entry in file {LogFile}",
                        logFile);

                    throw;
                }
            }

            return logs;
        }

        private FileStream GetFileStream(string logFile)
        {
            FileStream stream;

            try
            {
                stream = new(
                    logFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete);
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Could not read log file {LogFile}", logFile);
                throw;
            }

            return stream;
        }

        private void LoadLogs(StreamReader reader, string logFile, List<LogEntry> logs)
        {
            while (reader.ReadLine() is { } line)
            {
                try
                {
                    LogEntry? entry = JsonSerializer.Deserialize<LogEntry>(line);
                    if (entry != null)
                    {
                        logs.Add(entry);
                    }
                }
                catch (Exception e) when (e is ArgumentNullException or JsonException or NotSupportedException)
                {
                    _logger.LogWarning(
                        e,
                        "Could not read log entry {line} in file {LogFile}",
                        line,
                        logFile);
                }
            }
        }
    }
}
