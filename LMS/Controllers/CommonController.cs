using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
[assembly: InternalsVisibleTo( "LMSControllerTests" )]
namespace LMS.Controllers
{
    public class CommonController : Controller
    {
        private readonly LMSContext db;

        public CommonController(LMSContext _db)
        {
            db = _db;
        }

        /*******Begin code to modify********/

        /// <summary>
        /// Retreive a JSON array of all departments from the database.
        /// Each object in the array should have a field called "name" and "subject",
        /// where "name" is the department name and "subject" is the subject abbreviation.
        /// </summary>
        /// <returns>The JSON array</returns>
        public IActionResult GetDepartments()
        {         
            var departments = db.Departments
                .Select(d => new { subject = d.SubjectAbbr, name = d.Name })
                .ToList();
            return Json(departments);
        }
        
        /// <summary>
        /// Returns a JSON array representing the course catalog.
        /// Each object in the array should have the following fields:
        /// "subject": The subject abbreviation, (e.g. "CS")
        /// "dname": The department name, as in "Computer Science"
        /// "courses": An array of JSON objects representing the courses in the department.
        ///            Each field in this inner-array should have the following fields:
        ///            "number": The course number (e.g. 5530)
        ///            "cname": The course name (e.g. "Database Systems")
        /// </summary>
        /// <returns>The JSON array</returns>
        public IActionResult GetCatalog()
        {
            var departments = db.Departments.ToList();
            var courses = db.Courses.ToList();

            var catalog = departments.Select(d => new {
                subject = d.SubjectAbbr,
                dname = d.Name,
                courses = courses
                    .Where(c => c.DeptId == d.DeptId)
                    .Select(c => new {
                        number = c.Number,
                        cname = c.Name
                    })
                    .ToList()
            }).ToList();

            return Json(catalog);
        }

        /// <summary>
        /// Returns a JSON array of all class offerings of a specific course.
        /// Each object in the array should have the following fields:
        /// "season": the season part of the semester, such as "Fall"
        /// "year": the year part of the semester
        /// "location": the location of the class
        /// "start": the start time in format "hh:mm:ss"
        /// "end": the end time in format "hh:mm:ss"
        /// "fname": the first name of the professor
        /// "lname": the last name of the professor
        /// </summary>
        /// <param name="subject">The subject abbreviation, as in "CS"</param>
        /// <param name="number">The course number, as in 5530</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetClassOfferings(string subject, int number)
        {            
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) {
                return Json(Array.Empty<object>());
            }
    
            // lookup the course within the department
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == number);
            if (course == null) {
                return Json(Array.Empty<object>());
            }
    
            // lookup all class offerings joined with the professors
            var offerings = (from cls in db.Classes
                where cls.CourseId == course.CourseId
                join prof in db.Professors on cls.ProfessorId equals prof.UId
                select new {
                    season = cls.SemesterSeason,
                    year = cls.SemesterYear,
                    location = cls.Location,
                    start = cls.StartTime.ToString("HH:mm:ss"),
                    end = cls.EndTime.ToString("HH:mm:ss"),
                    fname = prof.FName,
                    lname = prof.LName
                }).ToList();
    
            return Json(offerings);
        }

        /// <summary>
        /// This method does NOT return JSON. It returns plain text (containing html).
        /// Use "return Content(...)" to return plain text.
        /// Returns the contents of an assignment.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment in the category</param>
        /// <returns>The assignment contents</returns>
        public IActionResult GetAssignmentContents(string subject, int num, string season, int year, string category, string asgname)
        {            
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null)
                return Content("");

            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null)
                return Content("");

            // lookup the class offering
            var classOffering = db.Classes.FirstOrDefault(cls =>
                cls.CourseId == course.CourseId &&
                cls.SemesterSeason.ToLower() == season.ToLower() &&
                cls.SemesterYear == (uint)year);
            if (classOffering == null)
                return Content("");

            // lookup the assignment category
            var assignCategory = db.AssignmentCategories.FirstOrDefault(ac =>
                ac.ClassId == classOffering.ClassId && ac.Name == category);
            if (assignCategory == null)
                return Content("");

            // lookup the assignment
            var assignment = db.Assignments.FirstOrDefault(a =>
                a.CategoryId == assignCategory.CategoryId && a.Name == asgname);
            if (assignment == null)
                return Content("");

            // done!
            return Content(assignment.Contents);
        }


        /// <summary>
        /// This method does NOT return JSON. It returns plain text (containing html).
        /// Use "return Content(...)" to return plain text.
        /// Returns the contents of an assignment submission.
        /// Returns the empty string ("") if there is no submission.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment in the category</param>
        /// <param name="uid">The uid of the student who submitted it</param>
        /// <returns>The submission text</returns>
        public IActionResult GetSubmissionText(string subject, int num, string season, int year, string category, string asgname, string uid)
        {            
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null)
                return Content("");

            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null)
                return Content("");

            // lookup the class offering
            var classOffering = db.Classes.FirstOrDefault(cls =>
                cls.CourseId == course.CourseId &&
                cls.SemesterSeason.ToLower() == season.ToLower() &&
                cls.SemesterYear == (uint)year);
            if (classOffering == null)
                return Content("");

            // lookup the assignment category
            var assignCategory = db.AssignmentCategories.FirstOrDefault(ac =>
                ac.ClassId == classOffering.ClassId && ac.Name == category);
            if (assignCategory == null)
                return Content("");

            // lookup the assignment
            var assignment = db.Assignments.FirstOrDefault(a =>
                a.CategoryId == assignCategory.CategoryId && a.Name == asgname);
            if (assignment == null)
                return Content("");

            // lookup the submission
            var submission = db.Submissions.FirstOrDefault(s =>
                s.AssignmentId == assignment.AssignmentId && s.UId == uid);
            if (submission == null)
                return Content("");

            return Content(submission.Contents);
        }


        /// <summary>
        /// Gets information about a user as a single JSON object.
        /// The object should have the following fields:
        /// "fname": the user's first name
        /// "lname": the user's last name
        /// "uid": the user's uid
        /// "department": (professors and students only) the name (such as "Computer Science") of the department for the user. 
        ///               If the user is a Professor, this is the department they work in.
        ///               If the user is a Student, this is the department they major in.    
        ///               If the user is an Administrator, this field is not present in the returned JSON
        /// </summary>
        /// <param name="uid">The ID of the user</param>
        /// <returns>
        /// The user JSON object 
        /// or an object containing {success: false} if the user doesn't exist
        /// </returns>
        public IActionResult GetUser(string uid)
        {           
            // lookup the student
            var student = db.Students.FirstOrDefault(s => s.UId == uid);
            if (student != null) {
                var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == student.Major.ToLower());
                return Json(new {
                    fname = student.FName,
                    lname = student.LName,
                    uid = student.UId,
                    department = dept != null ? dept.Name : student.Major
                });
            }

            // lookup the professor
            var professor = db.Professors.FirstOrDefault(p => p.UId == uid);
            if (professor != null) {
                var dept = db.Departments.FirstOrDefault(d => d.DeptId == professor.DepartmentId);
                return Json(new {
                    fname = professor.FName,
                    lname = professor.LName,
                    uid = professor.UId,
                    department = dept != null ? dept.Name : ""
                });
            }

            // lookup the admin
            var admin = db.Administrators.FirstOrDefault(a => a.UId == uid);
            if (admin != null) {
                return Json(new {
                    fname = admin.FName,
                    lname = admin.LName,
                    uid = admin.UId
                });
            }

            // done!
            return Json(new { success = false });
        }
        
        /*******End code to modify********/
    }
}