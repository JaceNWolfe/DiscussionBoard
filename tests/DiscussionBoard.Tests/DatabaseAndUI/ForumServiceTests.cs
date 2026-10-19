using DiscussionBoard.Tests.Infrastructure;

namespace DiscussionBoard.Tests.DatabaseAndUI
{
    public sealed class ForumServiceTests
    {
        [Fact(DisplayName = "ForumService - returns persisted forums")]
        public async Task ForumsAreRetrieved()
        {
            await using var fixture = await DatabaseFixture.CreateAsync();
            await using var db = fixture.CreateContext();
            await ServiceTestHarness.CreateForumAsync(db, "Forum Alpha");
            await ServiceTestHarness.CreateForumAsync(db, "Forum Beta");
            object service = ServiceTestHarness.Service("ForumService", db);
            object? result = await ServiceTestHarness.CallAsync(service, "GetAllAsync");

            var forums = ServiceTestHarness.AsItems(result, "GetAllAsync must return a collection of forums.");
            var names = forums.Select(f => f.GetType().GetProperty("Name")?.GetValue(f)?.ToString()).ToArray();

            Assert.True(forums.Count >= 2, "GetAllAsync must return persisted forums.");
            Assert.Contains("Forum Alpha", names);
            Assert.Contains("Forum Beta", names);
        }

        [Fact(DisplayName = "ForumService - empty database returns empty collection")]
        public async Task EmptyForums()
        {
            await using var fixture = await DatabaseFixture.CreateAsync();
            await using var db = fixture.CreateContext();
            object? result = await ServiceTestHarness.CallAsync(ServiceTestHarness.Service("ForumService", db), "GetAllAsync");

            Assert.Empty(ServiceTestHarness.AsItems(result, "GetAllAsync must return an empty collection when no forums exist."));
        }
    }
}
