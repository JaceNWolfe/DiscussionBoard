using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace DiscussionBoard.Models
{
    public class ApplicationUser : IdentityUser
    {

        public ICollection<DiscussionThread> DiscussionThreads { get; set; }
            = new List<DiscussionThread>();

        public ICollection<Post> Posts { get; set; }
            = new List<Post>();

    }
}
