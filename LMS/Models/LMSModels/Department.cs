using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class Department
    {
        public Department()
        {
            Administrators = new HashSet<Administrator>();
            Courses = new HashSet<Course>();
            Professors = new HashSet<Professor>();
        }

        public int DeptId { get; set; }
        public string Name { get; set; } = null!;
        public string SubjectAbbr { get; set; } = null!;

        public virtual ICollection<Administrator> Administrators { get; set; }
        public virtual ICollection<Course> Courses { get; set; }
        public virtual ICollection<Professor> Professors { get; set; }
    }
}
