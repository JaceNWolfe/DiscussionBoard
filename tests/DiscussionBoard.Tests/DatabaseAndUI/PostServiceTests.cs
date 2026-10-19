using DiscussionBoard.Tests.Infrastructure;

namespace DiscussionBoard.Tests.DatabaseAndUI
{
    public sealed class PostServiceTests
    {
        [Fact(DisplayName = "PostService - unknown thread has no posts")]
        public async Task UnknownThreadHasNoPosts()
        {
            await using var fixture = await DatabaseFixture.CreateAsync();
            await using var db = fixture.CreateContext();
            object? result = await ServiceTestHarness.CallAsync(
                ServiceTestHarness.Service("PostService", db), "GetForThreadAsync", -999);
            Assert.Empty(ServiceTestHarness.AsItems(result,
                "GetForThreadAsync must return no posts for an unknown thread."));
        }
    }
}
