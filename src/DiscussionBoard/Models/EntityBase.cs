using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DiscussionBoard.Models
{
    public abstract class EntityBase
    {
        public int Id { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
