using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class Assignment
    {
        public Assignment()
        {
            Submissions = new HashSet<Submission>();
        }

        public int AssignmentId { get; set; }
        public int ClassId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public uint? MaxPoints { get; set; }
        public string Contents { get; set; } = null!;
        public DateTime Due { get; set; }

        public virtual AssignmentCategory Category { get; set; } = null!;
        public virtual Class Class { get; set; } = null!;
        public virtual ICollection<Submission> Submissions { get; set; }
    }
}
