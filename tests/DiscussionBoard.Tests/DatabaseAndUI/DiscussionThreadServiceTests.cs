using DiscussionBoard.Tests.Infrastructure;

namespace DiscussionBoard.Tests.DatabaseAndUI
{
    public sealed class DiscussionThreadServiceTests
    {
        [Fact(DisplayName = "DiscussionThreadService - empty forums have no threads")]
        public async Task EmptyForumsHaveNoThreads()
        {
            await using var fixture = await DatabaseFixture.CreateAsync();
            await using var db = fixture.CreateContext();
            object first = await ServiceTestHarness.CreateForumAsync(db, "First");
            object second = await ServiceTestHarness.CreateForumAsync(db, "Second");
            object service = ServiceTestHarness.Service("DiscussionThreadService", db);
            foreach (object forum in new[] { first, second })
            {
                int id = Convert.ToInt32(forum.GetType().GetProperty("Id")!.GetValue(forum));
                object? result = await ServiceTestHarness.CallAsync(service, "GetForForumAsync", id);
                Assert.Empty(ServiceTestHarness.AsItems(result, "GetForForumAsync must return a collection of threads for the requested forum."));
            }
        }

        [Fact(DisplayName = "DiscussionThreadService - unknown thread returns no entity")]
        public async Task UnknownThread()
        {
            await using var fixture = await DatabaseFixture.CreateAsync();
            await using var db = fixture.CreateContext();
            object? result = await ServiceTestHarness.CallAsync(ServiceTestHarness.Service("DiscussionThreadService", db), "GetAsync", -999);
            Assert.Null(result);
        }
    }
}
