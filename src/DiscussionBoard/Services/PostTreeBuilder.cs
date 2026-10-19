using DiscussionBoard.Models;

namespace DiscussionBoard.Services
{
    /// <summary>Instructor-supplied in-memory tree assembly; students write the EF query, not this algorithm.</summary>
    public static class PostTreeBuilder
    {
        public sealed class Node(Post post)
        {
            public Post Post { get; } = post;
            public List<Node> Replies { get; } = new();
        }
        public static List<Node> Build(IEnumerable<Post> posts)
        {
            var roots = new List<Node>();
            var nodes = new Dictionary<int, Node>();

            foreach (Post post in posts)
            {
                nodes[post.Id] = new Node(post);
            }

            foreach (Node node in nodes.Values)
            {
                int? parentId = node.Post.ParentPostId;

                if (parentId.HasValue && // Build the post replies to other posts hierarchy
                    parentId.Value != node.Post.Id &&
                    nodes.TryGetValue(parentId.Value, out Node? parent))
                {
                    parent.Replies.Add(node);
                }
                else // Without a parent post, this is a reply directly to the discussion itself
                {
                    roots.Add(node);
                }
            }

            return roots.OrderBy(n => n.Post.CreatedAt).ThenBy(n => n.Post.Id).ToList();
        }
    }
}
