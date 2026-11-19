using DiscussionBoard.Models;
using DiscussionBoard.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Testing.Platform.Logging;

namespace DiscussionBoard.Tests.Starter;

public sealed class JsonLogReaderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DiscussionBoardTests", Guid.NewGuid().ToString("N"));

    private string LogDirectory => Path.Combine(_root, "Logs");

    [Fact]
    public void ReadsMultipleFilesAndLines()
    {
        WriteFile("discussionboard-20261008.json",
            """{"@t":"2026-10-08T10:00:00Z","@m":"First"}""",
            """{"@t":"2026-10-08T10:01:00Z","@l":"Warning","@m":"Second"}""");
        WriteFile("discussionboard-20261009.json",
            """{"@t":"2026-10-09T10:00:00Z","@l":"Error","@m":"Third"}""");

        List<LogEntry> entries = CreateReader().Read().ToList();

        Assert.Equal(3, entries.Count);
        Assert.Contains(entries, e => e.Message == "First" && e.Level == "Information");
        Assert.Contains(entries, e => e.Message == "Second" && e.Level == "Warning");
        Assert.Contains(entries, e => e.Message == "Third" && e.Level == "Error");
    }

    [Fact]
    public void RepeatedReadsDoNotAccumulateDuplicateEntries()
    {
        WriteFile("discussionboard-20261009.json",
            """{"@t":"2026-10-09T10:00:00Z","@m":"Only event"}""");
        JsonLogReader reader = CreateReader();

        Assert.Single(reader.Read());
        Assert.Single(reader.Read());
    }

    [Fact]
    public void MalformedLineDoesNotHideLaterValidEntries()
    {
        WriteFile("discussionboard-20261009.json",
            """{"@t":"2026-10-09T10:00:00Z","@m":"Before"}""",
            "{ invalid JSON",
            """{"@t":"2026-10-09T10:01:00Z","@m":"After"}""");

        List<LogEntry> entries = CreateReader().Read().ToList();

        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.Message == "Before");
        Assert.Contains(entries, e => e.Message == "After");
    }

    [Fact]
    public void EmptyDirectoryReturnsNoEntries()
    {
        Directory.CreateDirectory(LogDirectory);

        Assert.Empty(CreateReader().Read());
    }

    [Fact]
    public void MissingLogsDirectoryReturnsNoEntries()
    {
        // Arrange
        JsonLogReader reader = CreateReader();

        // Act
        List<LogEntry> entries = reader.Read().ToList();

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void ReadsFileWhileAnotherHandleRemainsOpenForWriting()
    {
        string path = WriteFile("discussionboard-20261009.json",
            """{"@t":"2026-10-09T10:00:00Z","@m":"Live"}""");
        using FileStream writer = new(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);

        Assert.Contains(CreateReader().Read(), e => e.Message == "Live");
    }

    private string WriteFile(string name, params string[] lines)
    {
        Directory.CreateDirectory(LogDirectory);
        string path = Path.Combine(LogDirectory, name);
        File.WriteAllLines(path, lines);
        return path;
    }

    private JsonLogReader CreateReader()
    {
        return new JsonLogReader(new TestWebHostEnvironment(_root), NullLogger<JsonLogReader>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private sealed class TestWebHostEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "DiscussionBoard";
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }


}
