using DiscussionBoard.Data;
using DiscussionBoard.Tests.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DiscussionBoard.Tests.DatabaseAndUI
{
    public sealed class MigrationExecutionTests
    {
        [Fact(DisplayName = "Migration - applies to a fresh isolated SQLite database")]
        public async Task MigrationsCreateDomainTables()
        {
            // Use a separate database: never migrate or alter the student's development database.
            await using SqliteConnection connection = new("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            await using ApplicationDbContext context = new(options);
            await context.Database.MigrateAsync(cancellationToken: TestContext.Current.CancellationToken);
            foreach (string entityName in new[] { "Forum", "DiscussionThread", "Post" })
            {
                Type type = ApplicationTypeLocator.RequireByName(entityName);
                string? table = context.Model.FindEntityType(type)?.GetTableName();
                Assert.False(string.IsNullOrWhiteSpace(table), $"The EF Core model must map {entityName}.");
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
                command.Parameters.AddWithValue("$name", table);
                Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
            }
        }
    }
}
