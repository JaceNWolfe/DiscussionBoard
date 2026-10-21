using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace DiscussionBoard.Models
{
    public class Forum : EntityBase
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public ICollection<DiscussionThread> DiscussionThreads { get; set; }
            = new List<DiscussionThread>();
    }
}
