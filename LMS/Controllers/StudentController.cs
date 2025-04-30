using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
[assembly: InternalsVisibleTo( "LMSControllerTests" )]
namespace LMS.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private LMSContext db;
        public StudentController(LMSContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Catalog()
        {
            return View();
        }

        public IActionResult Class(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            return View();
        }

        public IActionResult Assignment(string subject, string num, string season, string year, string cat, string aname)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            return View();
        }


        public IActionResult ClassListings(string subject, string num)
        {
            System.Diagnostics.Debug.WriteLine(subject + num);
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            return View();
        }


        /*******Begin code to modify********/

        /// <summary>
        /// Returns a JSON array of the classes the given student is enrolled in.
        /// Each object in the array should have the following fields:
        /// "subject" - The subject abbreviation of the class (such as "CS")
        /// "number" - The course number (such as 5530)
        /// "name" - The course name
        /// "season" - The season part of the semester
        /// "year" - The year part of the semester
        /// "grade" - The grade earned in the class, or "--" if one hasn't been assigned
        /// </summary>
        /// <param name="uid">The uid of the student</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetMyClasses(string uid)
        {           
            var myClasses = (from e in db.Enrollments
                join cls in db.Classes on e.ClassId equals cls.ClassId
                join course in db.Courses on cls.CourseId equals course.CourseId
                join dept in db.Departments on course.DeptId equals dept.DeptId
                where e.StudentId == uid
                select new {
                    subject = dept.SubjectAbbr,
                    number = course.Number,
                    name = course.Name,
                    season = cls.SemesterSeason,
                    year = cls.SemesterYear,
                    grade = string.IsNullOrEmpty(e.Grade) ? "--" : e.Grade
                }).ToList();
                     
            return Json(myClasses);
        }

        /// <summary>
        /// Returns a JSON array of all the assignments in the given class that the given student is enrolled in.
        /// Each object in the array should have the following fields:
        /// "aname" - The assignment name
        /// "cname" - The category name that the assignment belongs to
        /// "due" - The due Date/Time
        /// "score" - The score earned by the student, or null if the student has not submitted to this assignment.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="uid"></param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentsInClass(string subject, int num, string season, int year, string uid)
        {            
            // look up the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) return Json(Array.Empty<object>());
    
            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null) return Json(Array.Empty<object>());
    
            // look up the class offering
            var classOffering = db.Classes.FirstOrDefault(cls =>
                cls.CourseId == course.CourseId &&
                cls.SemesterSeason.ToLower() == season.ToLower() &&
                cls.SemesterYear == (uint)year);
            if (classOffering == null) return Json(Array.Empty<object>());
    
            // lookup assignments joined with category and submissions
            var assignments = (from cat in db.AssignmentCategories
                where cat.ClassId == classOffering.ClassId
                join asg in db.Assignments on cat.CategoryId equals asg.CategoryId
                // Left join submissions.
                join sub in db.Submissions.Where(s => s.UId == uid)
                    on asg.AssignmentId equals sub.AssignmentId into subGroup
                from submission in subGroup.DefaultIfEmpty()
                select new { 
                    aname = asg.Name,
                    cname = cat.Name,
                    due = asg.Due.ToString("yyyy-MM-dd HH:mm:ss"),
                    score = submission != null ? submission.Score : (double?)null
                }).ToList();
    
            return Json(assignments);
        }
        
        /// <summary>
        /// Adds a submission to the given assignment for the given student
        /// The submission should use the current time as its DateTime
        /// You can get the current time with DateTime.Now
        /// The score of the submission should start as 0 until a Professor grades it
        /// If a Student submits to an assignment again, it should replace the submission contents
        /// and the submission time (the score should remain the same).
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The new assignment name</param>
        /// <param name="uid">The student submitting the assignment</param>
        /// <param name="contents">The text contents of the student's submission</param>
        /// <returns>A JSON object containing {success = true/false}</returns>
        public IActionResult SubmitAssignmentText(string subject, int num, string season, int year,
          string category, string asgname, string uid, string contents)
        {           
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) return Json(new { success = false, message = "Department not found." });
            
            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null) return Json(new { success = false, message = "Course not found." });
            
            // lookup the class offering
            var classOffering = db.Classes.FirstOrDefault(cls =>
                cls.CourseId == course.CourseId &&
                cls.SemesterSeason.ToLower() == season.ToLower() &&
                cls.SemesterYear == (uint)year);
            if (classOffering == null) return Json(new { success = false, message = "Class offering not found." });
            
            // lookup the assignment category
            var assignCategory = db.AssignmentCategories.FirstOrDefault(ac =>
                ac.ClassId == classOffering.ClassId && ac.Name == category);
            if (assignCategory == null) return Json(new { success = false, message = "Assignment category not found." });
            
            // lookup the assignment
            var assignment = db.Assignments.FirstOrDefault(a =>
                a.CategoryId == assignCategory.CategoryId && a.Name == asgname);
            if (assignment == null) return Json(new { success = false, message = "Assignment not found." });
            
            // check if submission already exists
            var submission = db.Submissions.FirstOrDefault(s =>
                s.AssignmentId == assignment.AssignmentId && s.UId == uid);
            
            // update current submission
            if (submission != null) {
                submission.Contents = contents;
                submission.SubmissionTime = DateTime.Now;
                db.Submissions.Update(submission);
            }
            // add new submission
            else {
                submission = new Submission {
                    AssignmentId = assignment.AssignmentId,
                    UId = uid,
                    Contents = contents,
                    SubmissionTime = DateTime.Now,
                    Score = 0
                };
                db.Submissions.Add(submission);
            }
            db.SaveChanges();
            
            // done!
            return Json(new { success = true });
        }
        
        /// <summary>
        /// Enrolls a student in a class.
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester</param>
        /// <param name="year">The year part of the semester</param>
        /// <param name="uid">The uid of the student</param>
        /// <returns>A JSON object containing {success = {true/false}. 
        /// false if the student is already enrolled in the class, true otherwise.</returns>
        public IActionResult Enroll(string subject, int num, string season, int year, string uid)
        {          
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) return Json(new { success = false, message = "Department not found." });
    
            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null) return Json(new { success = false, message = "Course not found." });
    
            // lookup the class offering
            var classOffering = db.Classes.FirstOrDefault(cls =>
                cls.CourseId == course.CourseId &&
                cls.SemesterSeason.ToLower() == season.ToLower() &&
                cls.SemesterYear == (uint)year);
            if (classOffering == null) return Json(new { success = false, message = "Class offering not found." });
    
            // check if the enrollment already exists
            bool alreadyEnrolled = db.Enrollments.Any(e => e.StudentId == uid && e.ClassId == classOffering.ClassId);
            if (alreadyEnrolled) {
                return Json(new { success = false, message = "Already enrolled in this class." });
            }
    
            // enroll the student
            var newEnrollment = new Enrollment {
                StudentId = uid,
                ClassId = classOffering.ClassId,
                Grade = "--"
            };
            db.Enrollments.Add(newEnrollment);
            db.SaveChanges();
    
            // done!
            return Json(new { success = true });
        }

        // gpa mapping
        private double LetterGradeToGpa(string grade)
        {
            return grade switch
            {
                "A" => 4.0,
                "A-" => 3.7,
                "B+" => 3.3,
                "B" => 3.0,
                "B-" => 2.7,
                "C+" => 2.3,
                "C" => 2.0,
                "C-" => 1.7,
                "D+" => 1.3,
                "D" => 1.0,
                _ => 0.0
            };
        }
        
        /// <summary>
        /// Calculates a student's GPA
        /// A student's GPA is determined by the grade-point representation of the average grade in all their classes.
        /// Assume all classes are 4 credit hours.
        /// If a student does not have a grade in a class ("--"), that class is not counted in the average.
        /// If a student is not enrolled in any classes, they have a GPA of 0.0.
        /// Otherwise, the point-value of a letter grade is determined by the table on this page:
        /// https://advising.utah.edu/academic-standards/gpa-calculator-new.php
        /// </summary>
        /// <param name="uid">The uid of the student</param>
        /// <returns>A JSON object containing a single field called "gpa" with the number value</returns>
        public IActionResult GetGPA(string uid)
        {            
            // get all enrollments with value grades
            var enrollments = db.Enrollments.Where(e => e.StudentId == uid && e.Grade != "--").ToList();
    
            // no values found
            if (!enrollments.Any()) {
                return Json(new { gpa = 0.0 });
            }
    
            // calculate and return
            double totalPoints = enrollments.Sum(e => LetterGradeToGpa(e.Grade));
            double gpa = totalPoints / enrollments.Count;
            return Json(new { gpa = Math.Round(gpa, 2) });
        }
                
        /*******End code to modify********/
    }
}

