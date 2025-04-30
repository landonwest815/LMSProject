using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class Course
    {
        public Course()
        {
            Classes = new HashSet<Class>();
        }

        public int CourseId { get; set; }
        public string Name { get; set; } = null!;
        public uint Number { get; set; }
        public int DeptId { get; set; }

        public virtual Department Dept { get; set; } = null!;
        public virtual ICollection<Class> Classes { get; set; }
    }
}
