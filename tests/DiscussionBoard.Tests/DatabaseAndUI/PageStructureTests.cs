using AngleSharp.Html.Parser;
using DiscussionBoard.Tests.Infrastructure;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DiscussionBoard.Tests.DatabaseAndUI
{
    public sealed class PageStructureTests
    {
        private readonly ProjectFixture _project = new();

        [Theory(DisplayName = "Razor Pages - required view exists")]
        [InlineData("Forums/Details.cshtml", "Forum details page")]
        [InlineData("Discussions/Details.cshtml", "Discussion details page")]
        public void DetailPageExists(string relativePath, string description)
        {
            bool found = _project.FindApplicationFiles("*.cshtml")
                .Any(p => p.Replace('\\', '/')
                    .EndsWith($"Pages/{relativePath}", StringComparison.OrdinalIgnoreCase));

            Assert.True(found, $"The {description} (Pages/{relativePath}) must exist.");
        }

        [Theory(DisplayName = "Razor Pages - reusable partial exists")]
        [InlineData("_ThreadCard")]
        [InlineData("_PostTree")]
        public void PartialExists(string stem)
        {
            bool found = _project.FindApplicationFiles("*.cshtml")
                .Any(p => string.Equals(Path.GetFileNameWithoutExtension(p), stem, StringComparison.OrdinalIgnoreCase));
            Assert.True(found, $"The {stem}.cshtml partial view must exist.");
        }

        [Fact(DisplayName = "Input - thread form uses a dedicated bound input model")]
        public void ThreadFormUsesBoundInputModel()
        {
            // Discover by Razor PageModel binding, not by a prescribed student type name.
            // Binding to the entity itself is intentionally rejected: input must be mapped explicitly.
            Type entity = ApplicationTypeLocator.RequireByName("DiscussionThread");
            Type[] pageModels = ApplicationTypeLocator.ApplicationAssembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(Microsoft.AspNetCore.Mvc.RazorPages.PageModel).IsAssignableFrom(t))
                .ToArray();

            bool hasDedicatedInput = pageModels.SelectMany(t => t.GetProperties(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                .Where(p => Attribute.IsDefined(p, typeof(Microsoft.AspNetCore.Mvc.BindPropertyAttribute)))
                .Select(p => p.PropertyType)
                .Where(t => t != entity && t.IsClass && t != typeof(string))
                .Any(t => t.GetProperty("Title")?.PropertyType == typeof(string) &&
                          t.GetProperty("Content")?.PropertyType == typeof(string));

            Assert.True(hasDedicatedInput,
                "A thread create/edit PageModel must bind a dedicated input type with Title and Content; " +
                "its class name is your choice. Do not bind the EF Core entity directly.");
        }
    }
}
