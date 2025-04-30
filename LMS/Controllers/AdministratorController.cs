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
    public class AdministratorController : Controller
    {
        private readonly LMSContext db;

        public AdministratorController(LMSContext _db)
        {
            db = _db;
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Department(string subject)
        {
            ViewData["subject"] = subject;
            return View();
        }

        public IActionResult Course(string subject, string num)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            return View();
        }

        /*******Begin code to modify********/
        
        /// <summary>
        /// Create a department which is uniquely identified by it's subject code
        /// </summary>
        /// <param name="subject">the subject code</param>
        /// <param name="name">the full name of the department</param>
        /// <returns>A JSON object containing {success = true/false}.
        /// false if the department already exists, true otherwise.</returns>
        public IActionResult CreateDepartment(string subject, string name)
        {
            // return false if either input is empty
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(name)) {
                return Json(new { success = false });
            }
    
            // clean up any trailing whitespace left from the user
            subject = subject.Trim();
            name = name.Trim();
    
            // now check if department subject already exists
            if (db.Departments.Any(d => d.SubjectAbbr.ToLower() == subject.ToLower())) {
                return Json(new { success = false });
            }
    
            // create the new department and add it to the database
            var newDept = new Department {
                SubjectAbbr = subject,
                Name = name
            };
            db.Departments.Add(newDept);
            db.SaveChanges();
    
            // done!
            return Json(new { success = true });
        }
        
        /// <summary>
        /// Returns a JSON array of all the courses in the given department.
        /// Each object in the array should have the following fields:
        /// "number" - The course number (as in 5530)
        /// "name" - The course name (as in "Database Systems")
        /// </summary>
        /// <param name="subjCode">The department subject abbreviation (as in "CS")</param>
        /// <returns>The JSON result</returns>
        public IActionResult GetCourses(string subject)
        {
            // pulls out the number and name of all courses in the department
            var courses = db.Courses
                .Where(c => c.Dept.SubjectAbbr.ToLower() == subject.ToLower())
                .Select(c => new { number = c.Number, name = c.Name })
                .ToList();
            return Json(courses);
        }

        /// <summary>
        /// Returns a JSON array of all the professors working in a given department.
        /// Each object in the array should have the following fields:
        /// "lname" - The professor's last name
        /// "fname" - The professor's first name
        /// "uid" - The professor's uid
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <returns>The JSON result</returns>
        public IActionResult GetProfessors(string subject)
        {
            // pulls out the first, last name and uid of all professors in the department
            var professors = (from p in db.Professors
                    join d in db.Departments on p.DepartmentId equals d.DeptId
                    where d.SubjectAbbr.ToLower() == subject.ToLower()
                    select new { lname = p.LName, fname = p.FName, uid = p.UId })
                .ToList();
            return Json(professors);
        }
        
        /// <summary>
        /// Creates a course.
        /// A course is uniquely identified by its number + the subject to which it belongs
        /// </summary>
        /// <param name="subject">The subject abbreviation for the department in which the course will be added</param>
        /// <param name="number">The course number</param>
        /// <param name="name">The course name</param>
        /// <returns>A JSON object containing {success = true/false}.
        /// false if the course already exists, true otherwise.</returns>
        public IActionResult CreateCourse(string subject, int number, string name)
        {           
            // make sure neither input is missing/empty
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false });
            }

            // remove any whitespace possibly left by the user
            subject = subject.Trim();
            name = name.Trim();
    
            // look up the department + make sure it is not null
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) {
                return Json(new { success = false, message = "No department not found for the given subject." });
            }
    
            // check if the course to be created already exists
            bool exists = db.Courses.Any(c => c.DeptId == dept.DeptId && c.Number == number);
            if (exists) {
                return Json(new { success = false, message = "Course already exists." });
            }
    
            // create the course and add it to the database
            var newCourse = new Course {
                Name = name,
                Number = (uint)number,
                DeptId = dept.DeptId
            };
            db.Courses.Add(newCourse);
            db.SaveChanges();

            // done!
            return Json(new { success = true });
        }
        
        /// <summary>
        /// Creates a class offering of a given course.
        /// </summary>
        /// <param name="subject">The department subject abbreviation</param>
        /// <param name="number">The course number</param>
        /// <param name="season">The season part of the semester</param>
        /// <param name="year">The year part of the semester</param>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        /// <param name="location">The location</param>
        /// <param name="instructor">The uid of the professor</param>
        /// <returns>A JSON object containing {success = true/false}. 
        /// false if another class occupies the same location during any time 
        /// within the start-end range in the same semester, or if there is already
        /// a Class offering of the same Course in the same Semester,
        /// true otherwise.</returns>
        public IActionResult CreateClass(string subject, int number, string season, int year, DateTime start, DateTime end, string location, string instructor)
        {            
            // make sure all inputs are non-empty
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(season) ||
                string.IsNullOrWhiteSpace(location) || string.IsNullOrWhiteSpace(instructor)) {
                return Json(new { success = false, message = "Missing required information." });
            }
            
            // remove any possible whitespace
            subject = subject.Trim();
            season = season.Trim();
            location = location.Trim();

            // lookup the department; make sure it's there
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) {
                return Json(new { success = false, message = "No department found for the given subject." });
            }
            
            // lookup the course; make sure it's there
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == number);
            if (course == null) {
                return Json(new { success = false, message = "No course found for the given subject and number." });
            }
            
            // check if the course offering to be created already exists
            bool existingOffering = db.Classes.Any(cls =>
                cls.CourseId == course.CourseId &&
                cls.SemesterYear == (uint)year &&
                cls.SemesterSeason.ToLower() == season.ToLower());
            if (existingOffering) {
                return Json(new { success = false, message = "A class offering for this course already exists in the same semester." });
            }
            
            // check that times are in correct order
            TimeOnly newStart = TimeOnly.FromDateTime(start);
            TimeOnly newEnd   = TimeOnly.FromDateTime(end);
            if (newEnd <= newStart) {
                return Json(new { success = false, message = "End time must be after start time." });
            }
            
            // check for classes at the same location
            var classesAtLocation = db.Classes.Where(cls =>
                 cls.Location.ToLower() == location.ToLower() &&
                 cls.SemesterYear == (uint)year &&
                 cls.SemesterSeason.ToLower() == season.ToLower())
                 .ToList();
            
            // see if times overlap
            foreach (var cls in classesAtLocation)
            {
                if (!(newEnd <= cls.StartTime || newStart >= cls.EndTime)) {
                    return Json(new { success = false, message = "The location is occupied by another class during the specified time." });
                }
            }
            
            // create the class offering and add it to the database
            var newClass = new Class {
                CourseId = course.CourseId,
                ProfessorId = instructor,
                SemesterYear = (uint)year,
                SemesterSeason = season,
                Location = location,
                StartTime = newStart,
                EndTime = newEnd
            };
            db.Classes.Add(newClass);
            db.SaveChanges();
            
            return Json(new { success = true });
        }
        
        /*******End code to modify********/
    }
}