using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.Models.LMSModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
[assembly: InternalsVisibleTo( "LMSControllerTests" )]
namespace LMS_CustomIdentity.Controllers
{
    [Authorize(Roles = "Professor")]
    public class ProfessorController : Controller
    {

        private readonly LMSContext db;

        public ProfessorController(LMSContext _db)
        {
            db = _db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Students(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
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

        public IActionResult Categories(string subject, string num, string season, string year)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            return View();
        }

        public IActionResult CatAssignments(string subject, string num, string season, string year, string cat)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
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

        public IActionResult Submissions(string subject, string num, string season, string year, string cat, string aname)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            return View();
        }

        public IActionResult Grade(string subject, string num, string season, string year, string cat, string aname, string uid)
        {
            ViewData["subject"] = subject;
            ViewData["num"] = num;
            ViewData["season"] = season;
            ViewData["year"] = year;
            ViewData["cat"] = cat;
            ViewData["aname"] = aname;
            ViewData["uid"] = uid;
            return View();
        }

        /*******Begin code to modify********/
        
        /// <summary>
        /// Returns a JSON array of all the students in a class.
        /// Each object in the array should have the following fields:
        /// "fname" - first name
        /// "lname" - last name
        /// "uid" - user ID
        /// "dob" - date of birth
        /// "grade" - the student's grade in this class
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetStudentsInClass(string subject, int num, string season, int year)
        {
            // lookup department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) return Json(Array.Empty<object>());
            
            // lookup course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null) return Json(Array.Empty<object>());
            
            // lookup specific class offering
            var classOffering = db.Classes.FirstOrDefault(cls =>
                cls.CourseId == course.CourseId &&
                cls.SemesterSeason.ToLower() == season.ToLower() &&
                cls.SemesterYear == (uint)year);
            if (classOffering == null) return Json(Array.Empty<object>());

            // join students and enrollments
            var students = (from e in db.Enrollments
                join s in db.Students on e.StudentId equals s.UId
                where e.ClassId == classOffering.ClassId
                select new {
                    fname = s.FName,
                    lname = s.LName,
                    uid = s.UId,
                    dob = s.Dob.ToString(),
                    grade = string.IsNullOrEmpty(e.Grade) ? "--" : e.Grade
                }).ToList();
            return Json(students);
        }
        
        /// <summary>
        /// Returns a JSON array with all the assignments in an assignment category for a class.
        /// If the "category" parameter is null, return all assignments in the class.
        /// Each object in the array should have the following fields:
        /// "aname" - The assignment name
        /// "cname" - The assignment category name.
        /// "due" - The due DateTime
        /// "submissions" - The number of submissions to the assignment
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class, 
        /// or null to return assignments from all categories</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentsInCategory(string subject, int num, string season, int year, string category)
        {
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) return Json(Array.Empty<object>());

            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null) return Json(Array.Empty<object>());

            // lookup the class
            var cls = db.Classes.FirstOrDefault(cl => cl.CourseId == course.CourseId &&
                                                      cl.SemesterSeason.ToLower() == season.ToLower() &&
                                                      cl.SemesterYear == (uint)year);
            if (cls == null) return Json(Array.Empty<object>());

            // assignment categories
            IQueryable<AssignmentCategory> catQuery = db.AssignmentCategories.Where(ac => ac.ClassId == cls.ClassId);
            if (!string.IsNullOrEmpty(category))
                catQuery = catQuery.Where(ac => ac.Name.ToLower() == category.ToLower());

            // lookup assignments
            var assignments = (from ac in catQuery
                join a in db.Assignments on ac.CategoryId equals a.CategoryId
                select new {
                    aname = a.Name,
                    cname = ac.Name,
                    due = a.Due.ToString("yyyy-MM-dd HH:mm:ss"),
                    submissions = db.Submissions.Count(s => s.AssignmentId == a.AssignmentId)
                }).ToList();

            // done!
            return Json(assignments);
        }
        
        /// <summary>
        /// Returns a JSON array of the assignment categories for a certain class.
        /// Each object in the array should have the folling fields:
        /// "name" - The category name
        /// "weight" - The category weight
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetAssignmentCategories(string subject, int num, string season, int year)
        {
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) return Json(Array.Empty<object>());

            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null) return Json(Array.Empty<object>());

            // lookupt the class
            var cls = db.Classes.FirstOrDefault(cl => cl.CourseId == course.CourseId &&
                                                      cl.SemesterSeason.ToLower() == season.ToLower() &&
                                                      cl.SemesterYear == (uint)year);
            if (cls == null) return Json(Array.Empty<object>());

            // lookup the categories
            var categories = db.AssignmentCategories
                .Where(ac => ac.ClassId == cls.ClassId)
                .Select(ac => new { name = ac.Name, weight = ac.Weight })
                .ToList();

            // done!
            return Json(categories);
        }

        /// <summary>
        /// Creates a new assignment category for the specified class.
        /// If a category of the given class with the given name already exists, return success = false.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The new category name</param>
        /// <param name="catweight">The new category weight</param>
        /// <returns>A JSON object containing {success = true/false} </returns>
        public IActionResult CreateAssignmentCategory(string subject, int num, string season, int year, string category, int catweight)
        {
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) return Json(new { success = false, message = "Department not found." });

            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null) return Json(new { success = false, message = "Course not found." });

            // lookup the class
            var cls = db.Classes.FirstOrDefault(cl => cl.CourseId == course.CourseId &&
                                                      cl.SemesterSeason.ToLower() == season.ToLower() &&
                                                      cl.SemesterYear == (uint)year);
            if (cls == null) return Json(new { success = false, message = "Class offering not found." });

            // make sure category doesn't already exist
            bool exists = db.AssignmentCategories.Any(ac => ac.ClassId == cls.ClassId && ac.Name.ToLower() == category.ToLower());
            if (exists)
                return Json(new { success = false, message = "Category already exists." });

            // create the new category and add it to the database
            var newCat = new AssignmentCategory {
                ClassId = cls.ClassId,
                Name = category,
                Weight = (uint)catweight
            };
            db.AssignmentCategories.Add(newCat);
            db.SaveChanges();

            // done!
            return Json(new { success = true });
        }

        /// <summary>
        /// Creates a new assignment for the given class and category.
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The new assignment name</param>
        /// <param name="asgpoints">The max point value for the new assignment</param>
        /// <param name="asgdue">The due DateTime for the new assignment</param>
        /// <param name="asgcontents">The contents of the new assignment</param>
        /// <returns>A JSON object containing success = true/false</returns>
        public IActionResult CreateAssignment(string subject, int num, string season, int year, string category, string asgname, int asgpoints, DateTime asgdue, string asgcontents)
        {
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) 
                return Json(new { success = false, message = "Department not found." });

            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null) 
                return Json(new { success = false, message = "Course not found." });

            // lookup the class
            var cls = db.Classes.FirstOrDefault(cl =>
                cl.CourseId == course.CourseId &&
                cl.SemesterSeason.ToLower() == season.ToLower() &&
                cl.SemesterYear == (uint)year);
            if (cls == null) 
                return Json(new { success = false, message = "Class offering not found." });

            // lookup the assignment category
            var assignCategory = db.AssignmentCategories.FirstOrDefault(ac => 
                ac.ClassId == cls.ClassId && ac.Name.ToLower() == category.ToLower());
            if (assignCategory == null)
                return Json(new { success = false, message = "Assignment category not found." });

            // create the assignment and add it to the database
            var newAsg = new Assignment {
                ClassId = cls.ClassId,              
                CategoryId = assignCategory.CategoryId,
                Name = asgname,
                MaxPoints = (uint)asgpoints,
                Due = asgdue,
                Contents = asgcontents
            };
            db.Assignments.Add(newAsg);
            db.SaveChanges();

            // update all the grades
            UpdateGradesForClass(cls.ClassId);

            // done!
            return Json(new { success = true });
        }
        
        /// <summary>
        /// Gets a JSON array of all the submissions to a certain assignment.
        /// Each object in the array should have the following fields:
        /// "fname" - first name
        /// "lname" - last name
        /// "uid" - user ID
        /// "time" - DateTime of the submission
        /// "score" - The score given to the submission
        /// 
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetSubmissionsToAssignment(string subject, int num, string season, int year, string category, string asgname)
        {
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) return Json(Array.Empty<object>());

            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null) return Json(Array.Empty<object>());

            // lookup the class
            var cls = db.Classes.FirstOrDefault(cl => cl.CourseId == course.CourseId &&
                                                      cl.SemesterSeason.ToLower() == season.ToLower() &&
                                                      cl.SemesterYear == (uint)year);
            if (cls == null) return Json(Array.Empty<object>());

            // lookup the category
            var assignCategory = db.AssignmentCategories.FirstOrDefault(ac => ac.ClassId == cls.ClassId && ac.Name.ToLower() == category.ToLower());
            if (assignCategory == null) return Json(Array.Empty<object>());

            // lookup the assignment
            var assignment = db.Assignments.FirstOrDefault(a => a.CategoryId == assignCategory.CategoryId && a.Name.ToLower() == asgname.ToLower());
            if (assignment == null) return Json(Array.Empty<object>());

            // lookup the submissions
            var submissions = (from s in db.Submissions
                join stu in db.Students on s.UId equals stu.UId
                where s.AssignmentId == assignment.AssignmentId
                select new {
                    fname = stu.FName,
                    lname = stu.LName,
                    uid = stu.UId,
                    time = s.SubmissionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    score = s.Score
                }).ToList();

            // done!
            return Json(submissions);
        }
        
        /// <summary>
        /// Set the score of an assignment submission
        /// </summary>
        /// <param name="subject">The course subject abbreviation</param>
        /// <param name="num">The course number</param>
        /// <param name="season">The season part of the semester for the class the assignment belongs to</param>
        /// <param name="year">The year part of the semester for the class the assignment belongs to</param>
        /// <param name="category">The name of the assignment category in the class</param>
        /// <param name="asgname">The name of the assignment</param>
        /// <param name="uid">The uid of the student who's submission is being graded</param>
        /// <param name="score">The new score for the submission</param>
        /// <returns>A JSON object containing success = true/false</returns>
        public IActionResult GradeSubmission(string subject, int num, string season, int year, string category, string asgname, string uid, int score)
        {
            // lookup the department
            var dept = db.Departments.FirstOrDefault(d => d.SubjectAbbr.ToLower() == subject.ToLower());
            if (dept == null) return Json(new { success = false, message = "Department not found." });

            // lookup the course
            var course = db.Courses.FirstOrDefault(c => c.DeptId == dept.DeptId && c.Number == num);
            if (course == null) return Json(new { success = false, message = "Course not found." });

            // lookup the class
            var cls = db.Classes.FirstOrDefault(cl => cl.CourseId == course.CourseId &&
                                                      cl.SemesterSeason.ToLower() == season.ToLower() &&
                                                      cl.SemesterYear == (uint)year);
            if (cls == null) return Json(new { success = false, message = "Class offering not found." });

            // lookup the category
            var assignCategory = db.AssignmentCategories.FirstOrDefault(ac => ac.ClassId == cls.ClassId && ac.Name.ToLower() == category.ToLower());
            if (assignCategory == null) return Json(new { success = false, message = "Assignment category not found." });

            // lookup the assignment
            var assignment = db.Assignments.FirstOrDefault(a => a.CategoryId == assignCategory.CategoryId && a.Name.ToLower() == asgname.ToLower());
            if (assignment == null) return Json(new { success = false, message = "Assignment not found." });

            // look up the submission
            var submission = db.Submissions.FirstOrDefault(s => s.AssignmentId == assignment.AssignmentId && s.UId == uid);
            if (submission == null) return Json(new { success = false, message = "Submission not found." });
            
            // update its score and save the changes to the database
            submission.Score = (uint)score;
            db.Submissions.Update(submission);
            db.SaveChanges();

            // update grade
            UpdateGradeForStudent(cls.ClassId, uid);

            // done!
            return Json(new { success = true });
        }
        
        /// <summary>
        /// Returns a JSON array of the classes taught by the specified professor
        /// Each object in the array should have the following fields:
        /// "subject" - The subject abbreviation of the class (such as "CS")
        /// "number" - The course number (such as 5530)
        /// "name" - The course name
        /// "season" - The season part of the semester in which the class is taught
        /// "year" - The year part of the semester in which the class is taught
        /// </summary>
        /// <param name="uid">The professor's uid</param>
        /// <returns>The JSON array</returns>
        public IActionResult GetMyClasses(string uid)
        {            
            var classes = (from cls in db.Classes
                where cls.ProfessorId == uid
                join course in db.Courses on cls.CourseId equals course.CourseId
                join dept in db.Departments on course.DeptId equals dept.DeptId
                select new {
                    subject = dept.SubjectAbbr,
                    number = course.Number,
                    name = course.Name,
                    season = cls.SemesterSeason,
                    year = cls.SemesterYear
                }).ToList();
            return Json(classes);
        }

        // update grades for ALL students in a class
        private void UpdateGradesForClass(int classId)
        {
            // get all class enrollments
            var enrollments = db.Enrollments.Where(e => e.ClassId == classId).ToList();

            // look at each one
            foreach (var e in enrollments)
            {
                // get all categories
                var categories = db.AssignmentCategories.Where(ac => ac.ClassId == classId).ToList();
                double totalWeightedScore = 0;
                double totalWeight = 0;

                // look at each category
                foreach (var cat in categories)
                {
                    // get all assignments in said category
                    var assignments = db.Assignments.Where(a => a.CategoryId == cat.CategoryId).ToList();
                    if (!assignments.Any())
                        continue;

                    double earned = 0;
                    double possible = 0;
                    
                    // look at each assignment and add up score and possible score
                    foreach (var a in assignments) {
                        var submission = db.Submissions.FirstOrDefault(s => s.AssignmentId == a.AssignmentId && s.UId == e.StudentId);
                        double score = submission != null ? submission.Score : 0;
                        earned += score;
                        possible += (double)a.MaxPoints;
                    }

                    // check to make sure there is at least one graded assignment
                    // add weights
                    if (possible > 0) {
                        double percentage = earned / possible;
                        totalWeightedScore += percentage * cat.Weight;
                        totalWeight += cat.Weight;
                    }
                }

                // determine if grade is actually computed or not
                if (totalWeight == 0)
                    e.Grade = "--";
                else
                {
                    double finalPercentage = totalWeightedScore * (100.0 / totalWeight);
                    e.Grade = ConvertPercentageToLetter(finalPercentage);
                }
            }
            db.SaveChanges();
        }

        // update grade for a SINGLE student in a class
        private void UpdateGradeForStudent(int classId, string uid)
        {
            // get the student's enrollment
            var enrollment = db.Enrollments.FirstOrDefault(e => e.ClassId == classId && e.StudentId == uid);
            if (enrollment == null)
                return;

            // get all categories
            var categories = db.AssignmentCategories.Where(ac => ac.ClassId == classId).ToList();
            double totalWeightedScore = 0;
            double totalWeight = 0;

            // look at each category
            foreach (var cat in categories)
            {
                // get all assignments
                var assignments = db.Assignments.Where(a => a.CategoryId == cat.CategoryId).ToList();
                if (!assignments.Any())
                    continue;

                double earned = 0;
                double possible = 0;
                
                // look at each assignment and add up scores and possible scores
                foreach (var a in assignments) {
                    var submission = db.Submissions.FirstOrDefault(s => s.AssignmentId == a.AssignmentId && s.UId == uid);
                    double score = submission != null ? submission.Score : 0;
                    earned += score;
                    possible += (double)a.MaxPoints;
                }

                // check that assignment is not a no count
                if (possible > 0) {
                    double percentage = earned / possible;
                    totalWeightedScore += percentage * cat.Weight;
                    totalWeight += cat.Weight;
                }
            }

            // determine if grade should actually be computed
            if (totalWeight == 0)
                enrollment.Grade = "--";
            else {
                double finalPercentage = totalWeightedScore * (100.0 / totalWeight);
                enrollment.Grade = ConvertPercentageToLetter(finalPercentage);
            }
            db.SaveChanges();
        }

        // letter grade mapping
        private string ConvertPercentageToLetter(double percentage)
        {
            return percentage switch
            {
                >= 93.0 => "A",
                >= 90.0 => "A-",
                >= 87.0 => "B+",
                >= 83.0 => "B",
                >= 80.0 => "B-",
                >= 77.0 => "C+",
                >= 73.0 => "C",
                >= 70.0 => "C-",
                >= 67.0 => "D+",
                >= 63.0 => "D",
                >= 60.0 => "D-",
                _ => "F"
            };
        }
        
        /*******End code to modify********/
    }
}