using System;
using System.Collections.Generic;

namespace LMS.Models.LMSModels
{
    public partial class Class
    {
        public Class()
        {
            AssignmentCategories = new HashSet<AssignmentCategory>();
            Assignments = new HashSet<Assignment>();
            Enrollments = new HashSet<Enrollment>();
        }

        public int ClassId { get; set; }
        public int CourseId { get; set; }
        public string ProfessorId { get; set; } = null!;
        public uint SemesterYear { get; set; }
        public string SemesterSeason { get; set; } = null!;
        public string Location { get; set; } = null!;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        public virtual Course Course { get; set; } = null!;
        public virtual Professor Professor { get; set; } = null!;
        public virtual ICollection<AssignmentCategory> AssignmentCategories { get; set; }
        public virtual ICollection<Assignment> Assignments { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; }
    }
}
