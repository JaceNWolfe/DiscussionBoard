using DiscussionBoard.Data;
using DiscussionBoard.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DiscussionBoard.Tests.DatabaseAndUI
{

    public sealed class EfCoreMappingTests
    {
        [Theory(DisplayName = "EF Core - domain entity is mapped")]
        [InlineData("Forum")]
        [InlineData("DiscussionThread")]
        [InlineData("Post")]
        public async Task EntityIsMapped(string name)
        {
            await using DatabaseFixture fixture = await DatabaseFixture.CreateAsync();
            await using ApplicationDbContext context = fixture.CreateContext();
            EfModelAssert.RequireEntity(context, name);
        }

        [Theory(DisplayName = "EF Core - DbSet is exposed")]
        [InlineData("Forums", "Forum")]
        [InlineData("DiscussionThreads", "DiscussionThread")]
        [InlineData("Posts", "Post")]
        public void DbSetExists(string property, string entity)
        {
            Type type = ApplicationTypeLocator.RequireByName(entity);
            var info = typeof(ApplicationDbContext).GetProperty(property);
            Assert.True(info is not null && info.PropertyType == typeof(DbSet<>).MakeGenericType(type),
                $"ApplicationDbContext must expose DbSet<{entity}> {property}.");
        }

        [Theory(DisplayName = "EF Core - relationship is mapped")]
        [InlineData("DiscussionThread", "Forum", "ForumId", false)]
        [InlineData("Post", "DiscussionThread", "DiscussionThreadId", false)]
        [InlineData("Post", "Post", "ParentPostId", true)]
        [InlineData("DiscussionThread", "ApplicationUser", "ApplicationUserId", false)]
        [InlineData("Post", "ApplicationUser", "ApplicationUserId", false)]
        public async Task ForeignKeyIsMapped(string dependent, string principal, string key, bool optional)
        {
            await using DatabaseFixture fixture = await DatabaseFixture.CreateAsync();
            await using ApplicationDbContext context = fixture.CreateContext();
            IEntityType entity = EfModelAssert.RequireEntity(context, dependent);
            Type principalType = ApplicationTypeLocator.RequireByName(principal);
            IForeignKey? fk = entity.GetForeignKeys().FirstOrDefault(candidate =>
                candidate.PrincipalEntityType.ClrType == principalType &&
                candidate.Properties.Any(p => p.Name == key));
            Assert.True(fk is not null, $"EF Core must map {dependent}.{key} to {principal}.");
            Assert.Equal(optional, !fk.IsRequired);
        }

        [Fact(DisplayName = "EF Core - new database creates discussion tables")]
        public async Task NewDatabaseCreatesTables()
        {
            await using DatabaseFixture fixture = await DatabaseFixture.CreateAsync();
            await using ApplicationDbContext context = fixture.CreateContext();
            foreach (string name in new[] { "Forum", "DiscussionThread", "Post" })
            {
                IEntityType entity = EfModelAssert.RequireEntity(context, name);
                string table = entity.GetTableName() ?? "";
                Assert.False(string.IsNullOrWhiteSpace(table), $"{name} needs a database table.");
                var connection = context.Database.GetDbConnection();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$table";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "$table";
                parameter.Value = table;
                command.Parameters.Add(parameter);
                Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken)));
            }
        }
    }
}
