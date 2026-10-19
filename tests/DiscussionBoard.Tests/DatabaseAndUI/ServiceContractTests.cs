using System.Reflection;
using DiscussionBoard.Tests.Infrastructure;

namespace DiscussionBoard.Tests.DatabaseAndUI
{
    public sealed class ServiceContractTests
    {
        [Theory(DisplayName = "Service - required asynchronous method exists")]
        [InlineData("ForumService", "GetAllAsync", "Forum", false, false)]
        [InlineData("DiscussionThreadService", "GetAsync", "DiscussionThread", true, false)]
        [InlineData("DiscussionThreadService", "GetForForumAsync", "DiscussionThread", true, true)]
        [InlineData("DiscussionThreadService", "AddAsync", "DiscussionThread", false, false)]
        [InlineData("DiscussionThreadService", "UpdateAsync", "DiscussionThread", false, false)]
        [InlineData("PostService", "GetForThreadAsync", "Post", true, true)]
        [InlineData("PostService", "AddAsync", "Post", false, false)]
        public void RequiredMethod(string service, string method, string entity, bool integerArgument, bool returnsCollection)
        {
            Type serviceType = ApplicationTypeLocator.RequireByName(service);
            Type entityType = ApplicationTypeLocator.RequireByName(entity);
            MethodInfo[] methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == method).ToArray();
            Assert.NotEmpty(methods);
            Type expectedParameter = integerArgument ? typeof(int) : entityType;
            Assert.True(methods.Any(m =>
                    (method == "GetAllAsync" && m.GetParameters().Length == 0) ||
                    (m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == expectedParameter)),
                $"{service}.{method} must accept the documented argument (or none for GetAllAsync).");
            Assert.True(methods.Any(m => typeof(Task).IsAssignableFrom(m.ReturnType)),
                $"{service}.{method} must be asynchronous (return a Task).");
            if (returnsCollection)
            {
                Assert.True(methods.Any(m => m.ReturnType.IsGenericType &&
                                             m.ReturnType.GenericTypeArguments.Length == 1 &&
                                             typeof(System.Collections.IEnumerable).IsAssignableFrom(m.ReturnType.GenericTypeArguments[0])),
                    $"{service}.{method} must return a Task containing a collection.");
            }
        }
    }
}
