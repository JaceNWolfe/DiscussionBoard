using System.Collections;
using System.Reflection;
using DiscussionBoard.Tests.Infrastructure;

namespace DiscussionBoard.Tests.DatabaseAndUI
{
    public sealed class ModelRelationshipTests
    {
        private static Type T(string name) => ApplicationTypeLocator.RequireByName(name);

        [Theory(DisplayName = "Model - collection navigation exists")]
        [InlineData("Forum", "DiscussionThreads", "DiscussionThread")]
        [InlineData("DiscussionThread", "Posts", "Post")]
        [InlineData("Post", "Replies", "Post")]
        public void CollectionNavigation(string owner, string property, string element)
        {
            Type ownerType = T(owner);
            Type elementType = T(element);
            PropertyInfo? info = ownerType.GetProperty(property);
            Assert.True(info is not null, $"{owner}.{property} must be a public collection navigation.");
            Assert.True(info.PropertyType != typeof(string) &&
                        info.PropertyType.GetInterfaces().Append(info.PropertyType).Any(i =>
                            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                            i.GenericTypeArguments[0] == elementType),
                $"{owner}.{property} must be a collection of {element} objects.");
        }

        [Fact(DisplayName = "Model - thread author navigation")]
        public void ThreadAuthor() => ReflectionAssert.HasProperty(T("DiscussionThread"), "ApplicationUser", T("ApplicationUser"));

        [Fact(DisplayName = "Model - post author navigation")]
        public void PostAuthor() => ReflectionAssert.HasProperty(T("Post"), "ApplicationUser", T("ApplicationUser"));

        [Fact(DisplayName = "Model - nullable parent allows root posts")]
        public void NullableParent() => ReflectionAssert.HasProperty<int?>(T("Post"), "ParentPostId");

        [Fact(DisplayName = "Model - author foreign key")]
        public void AuthorForeignKey() => ReflectionAssert.HasProperty<string>(T("DiscussionBase"), "ApplicationUserId");
    }
}
