using DiscussionBoard.Tests.Infrastructure;

namespace DiscussionBoard.Tests.DatabaseAndUI
{
    public sealed class RepositoryTests
    {
        private readonly ProjectFixture _project = new();

        [Theory(DisplayName = "Repository - generated SQLite files are ignored")]
        [InlineData("src/DiscussionBoard/Data/discussion.db")]
        [InlineData("src/DiscussionBoard/Data/discussion.db-shm")]
        [InlineData("src/DiscussionBoard/Data/discussion.db-wal")]
        public void GeneratedDatabaseFilesAreIgnored(string path)
        {
            GitRepositoryInspector git = new(_project.RepositoryRoot);

            Assert.True(
                git.IsIgnored(path),
                $"Generated database file '{path}' must be ignored by Git. Commit migrations, not the development database.");
        }

        [Fact(DisplayName = "Repository - migration source exists")]
        public void MigrationSourceExists()
        {
            string migrationsDirectory = _project.GetApplicationPath(Path.Combine("Data", "Migrations"));
            bool hasMigration = Directory.Exists(migrationsDirectory) &&
                                Directory.EnumerateFiles(migrationsDirectory, "*.cs", SearchOption.TopDirectoryOnly)
                                    .Any(path => !path.EndsWith("ModelSnapshot.cs", StringComparison.Ordinal));

            Assert.True(
                hasMigration,
                "The application must contain at least one EF Core migration source file. Migration history is source code; the SQLite database is generated state.");
        }
    }
}
