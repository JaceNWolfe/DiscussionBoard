namespace DiscussionBoard.Services;

/// <summary>Application-owned avatar choices. Only IDs from this catalog may be persisted.</summary>
public static class AvatarCatalog
{
    public const string DefaultId = "default";

    public sealed record AvatarOption(string Id, string DisplayName);

    private static readonly IReadOnlyList<AvatarOption> OptionsInternal =
        Array.AsReadOnly(new[]
        {
            new AvatarOption("default", "Default"),
            new AvatarOption("astronaut", "Astronaut"),
            new AvatarOption("robot", "Robot"),
            new AvatarOption("fox", "Fox"),
            new AvatarOption("owl", "Owl")
        });

    public static IReadOnlyList<AvatarOption> Options => OptionsInternal;

    public static bool IsValid(string? id) =>
        id is not null && OptionsInternal.Any(option => option.Id == id);

    public static string ImagePath(string? id) =>
        $"/images/avatars/{(IsValid(id) ? id : DefaultId)}.svg";
}
