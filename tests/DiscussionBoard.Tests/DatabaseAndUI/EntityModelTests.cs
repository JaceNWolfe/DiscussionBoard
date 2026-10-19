using System.ComponentModel.DataAnnotations;
using DiscussionBoard.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace DiscussionBoard.Tests.DatabaseAndUI
{
    public sealed class EntityModelTests
    {
        [Fact(DisplayName = "Entity Model - ApplicationUser uses Identity")]
        public void ApplicationUserUsesIdentity()
        {
            Type applicationUser = ApplicationTypeLocator.RequireByName("ApplicationUser");
            ReflectionAssert.InheritsFrom(applicationUser, typeof(IdentityUser));
        }

        [Fact(DisplayName = "Entity Model - EntityBase")]
        public void EntityBaseHasRequiredProperties()
        {
            Type entityBase = ApplicationTypeLocator.RequireByName("EntityBase");
            ReflectionAssert.HasProperty<int>(entityBase, "Id");
            ReflectionAssert.HasProperty<bool>(entityBase, "IsDeleted");
        }

        [Fact(DisplayName = "Entity Model - Forum")]
        public void ForumHasRequiredProperties()
        {
            Type forum = ApplicationTypeLocator.RequireByName("Forum");
            Type entityBase = ApplicationTypeLocator.RequireByName("EntityBase");
            ReflectionAssert.InheritsFrom(forum, entityBase);
            ReflectionAssert.HasProperty<string>(forum, "Name");
            ReflectionAssert.HasProperty<string>(forum, "Description");
        }

        [Fact(DisplayName = "Entity Model - DiscussionBase")]
        public void DiscussionBaseHasRequiredProperties()
        {
            Type discussionBase = ApplicationTypeLocator.RequireByName("DiscussionBase");
            Type entityBase = ApplicationTypeLocator.RequireByName("EntityBase");
            Type applicationUser = ApplicationTypeLocator.RequireByName("ApplicationUser");

            ReflectionAssert.InheritsFrom(discussionBase, entityBase);
            var title = ReflectionAssert.HasProperty<string>(discussionBase, "Title");
            ReflectionAssert.HasAttribute<MaxLengthAttribute>(title);
            ReflectionAssert.HasProperty<string>(discussionBase, "Content");
            ReflectionAssert.HasProperty<DateTimeOffset>(discussionBase, "CreatedAt");
            ReflectionAssert.HasProperty<string>(discussionBase, "ApplicationUserId");
            ReflectionAssert.HasProperty(discussionBase, "ApplicationUser", applicationUser);
        }

        [Fact(DisplayName = "Entity Model - DiscussionThread")]
        public void DiscussionThreadHasRequiredRelationships()
        {
            Type thread = ApplicationTypeLocator.RequireByName("DiscussionThread");
            Type discussionBase = ApplicationTypeLocator.RequireByName("DiscussionBase");
            Type forum = ApplicationTypeLocator.RequireByName("Forum");

            ReflectionAssert.InheritsFrom(thread, discussionBase);
            ReflectionAssert.HasProperty<int>(thread, "ForumId");
            ReflectionAssert.HasProperty(thread, "Forum", forum);
        }

        [Fact(DisplayName = "Entity Model - Post")]
        public void PostHasRequiredRelationships()
        {
            Type post = ApplicationTypeLocator.RequireByName("Post");
            Type discussionBase = ApplicationTypeLocator.RequireByName("DiscussionBase");
            Type thread = ApplicationTypeLocator.RequireByName("DiscussionThread");

            ReflectionAssert.InheritsFrom(post, discussionBase);
            ReflectionAssert.HasProperty<int>(post, "DiscussionThreadId");
            ReflectionAssert.HasProperty(post, "DiscussionThread", thread);
            ReflectionAssert.HasProperty(post, "ParentPostId", typeof(int?));
            ReflectionAssert.HasProperty(post, "ParentPost", post);
        }
    }
}
