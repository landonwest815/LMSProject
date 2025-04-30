using System;
using System.Linq;
using System.Reflection;
using LMS.Controllers;
using LMS.Models.LMSModels;
using LMS_CustomIdentity.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LMSControllerTests
{
    public class UnitTest1
    {
        [Fact]
        public void Test_GetDepartments()
        {
            // setup
            var db = MakeTinyDB();
            var ctrl = new CommonController(db);
            
            // retrieve data
            var result = ctrl.GetDepartments() as JsonResult;
            dynamic depts = result.Value;
            
            // check it
            Assert.Equal(1, (int)depts.Count);   // should just be one department
            Assert.Equal("CS", (string)depts[0].subject);   // it should be CS
            Assert.Equal("Computer Science", (string)depts[0].name);
        }

        [Fact]
        public void Test_GetCatalog()
        {
            // setup
            var db = MakeTinyDB();
            var ctrl = new CommonController(db);
            
            // retrieve data
            var result = ctrl.GetCatalog() as JsonResult;
            dynamic catalog = result.Value;
            
            // check it
            Assert.Equal("CS", (string)catalog[0].subject);
            Assert.Equal("Computer Science", (string)catalog[0].dname);
            Assert.Equal(5530, (int)catalog[0].courses[0].number);
            Assert.Equal("Database Systems", (string)catalog[0].courses[0].cname);
        }

        [Fact]
        public void Test_GetClassOfferings()
        {
            // setup
            var db = MakeTinyDB();
            var ctrl = new CommonController(db);
            
            // retrieve data
            var result = ctrl.GetClassOfferings("CS", 5530) as JsonResult;
            dynamic offerings = result.Value;
            
            // check it
            Assert.Equal("Fall", (string)offerings[0].season);
            Assert.Equal(2025, (int)offerings[0].year);
            Assert.Equal("WEB", (string)offerings[0].location);
            Assert.Equal("09:00:00", (string)offerings[0].start);
            Assert.Equal("10:20:00", (string)offerings[0].end);
        }

        [Fact]
        public void Test_GetAssignmentContents()
        {
            // setup
            var db = MakeTinyDB();
            var ctrl = new CommonController(db);
            
            // retrieve data
            var result = ctrl.GetAssignmentContents("CS", 5530, "Fall", 2025, "Homework", "HW1") as ContentResult;
            
            // check it
            Assert.NotNull(result);
            Assert.Equal("This is Homework 1", result.Content);
        }

        [Fact]
        public void Test_GetSubmissionText()
        {
            // setup
            var db = MakeTinyDB();
            var ctrl = new CommonController(db);
            
            // retrieve data
            var result = ctrl.GetSubmissionText("CS", 5530, "Fall", 2023, "Homework", "HW1", "u0000003") as ContentResult;
            
            // check it
            Assert.Equal("", result.Content);
        }

        [Fact]
        public void Test_GetStudentInfo()
        {
            // setup
            var db = MakeTinyDB();
            var ctrl = new CommonController(db);
            
            // retrieve data
            var result = ctrl.GetUser("u0000003") as JsonResult;
            dynamic user = result.Value;
            
            // check it
            Assert.Equal("u0000003", (string)user.uid);
            Assert.Equal("Landon", user.fname);
            Assert.Equal("West", user.lname);
            Assert.Equal("Computer Science", (string)user.department);
        }

        // ProfessorController auto-grading tests

        private void CallUpdateGradeForStudent(ProfessorController ctrl, int classId, string uid)
        {
            var method = typeof(ProfessorController).GetMethod("UpdateGradeForStudent", BindingFlags.NonPublic | BindingFlags.Instance);
            method.Invoke(ctrl, new object[] { classId, uid });
        }

        [Fact]
        public void Test_NoAssignments_GradeRemainsUndecided()
        {
            // setup
            var db = MakeTinyDB();
            var profCtrl = new ProfessorController(db);
            
            // adjust data
            db.Assignments.RemoveRange(db.Assignments);
            db.SaveChanges();
            CallUpdateGradeForStudent(profCtrl, 1, "u0000003");
            var enrollment = db.Enrollments.First(e => e.StudentId == "u0000003" && e.ClassId == 1);
            
            // check it
            Assert.Equal("--", enrollment.Grade);
        }

        [Fact]
        public void Test_FullCredit()
        {
            // setup
            var db = MakeTinyDB();
            
            // adjust data
            var submission = new Submission
            {
                SubmissionId = 1,
                AssignmentId = 1,
                UId = "u0000003",
                Contents = "Complete answers",
                SubmissionTime = DateTime.Now,
                Score = 100
            };
            db.Submissions.Add(submission);
            db.SaveChanges();
            var profCtrl = new ProfessorController(db);
            CallUpdateGradeForStudent(profCtrl, 1, "u0000003");
            var enrollment = db.Enrollments.First(e => e.StudentId == "u0000003" && e.ClassId == 1);
            
            // check it
            Assert.Equal("A", enrollment.Grade);
        }

        [Fact]
        public void Test_PartialCredit()
        {
            // setup
            var db = MakeTinyDB();
            
            // adjust data
            var existing = db.Submissions.Where(s => s.AssignmentId == 1 && s.UId == "u0000003");
            db.Submissions.RemoveRange(existing);
            db.SaveChanges();
            var submission = new Submission
            {
                SubmissionId = 2,
                AssignmentId = 1,
                UId = "u0000003",
                Contents = "Partial work",
                SubmissionTime = DateTime.Now,
                Score = 70
            };
            db.Submissions.Add(submission);
            db.SaveChanges();
            var profCtrl = new ProfessorController(db);
            CallUpdateGradeForStudent(profCtrl, 1, "u0000003");
            var enrollment = db.Enrollments.First(e => e.StudentId == "u0000003" && e.ClassId == 1);
            
            // check it
            Assert.Equal("B-", enrollment.Grade);
        }

        [Fact]
        public void Test_MultipleCategories()
        {
            // setup
            var db = MakeTinyDB();
            
            // quizzes
            var catQuizzes = new AssignmentCategory
            {
                CategoryId = 3,
                ClassId = 1,
                Name = "Quizzes",
                Weight = 10
            };
            db.AssignmentCategories.Add(catQuizzes);
            db.SaveChanges();
            
            // quiz
            var quizAssignment = new Assignment
            {
                AssignmentId = 2,
                CategoryId = catQuizzes.CategoryId,
                Name = "Quiz1",
                MaxPoints = 10,
                Due = DateTime.Today.AddDays(14),
                Contents = "Quiz description"
            };
            db.Assignments.Add(quizAssignment);
            db.SaveChanges();
            
            // homework submission
            var subHomework = new Submission
            {
                SubmissionId = 3,
                AssignmentId = 1,
                UId = "u0000003",
                Contents = "Homework complete",
                SubmissionTime = DateTime.Now,
                Score = 100
            };
            db.Submissions.Add(subHomework);
            
            // quiz submission
            var subQuiz = new Submission
            {
                SubmissionId = 4,
                AssignmentId = 2,
                UId = "u0000003",
                Contents = "Quiz work",
                SubmissionTime = DateTime.Now,
                Score = 8
            };
            db.Submissions.Add(subQuiz);
            
            // update everything
            db.SaveChanges();
            var profCtrl = new ProfessorController(db);
            CallUpdateGradeForStudent(profCtrl, 1, "u0000003");
            var enrollment = db.Enrollments.First(e => e.StudentId == "u0000003" && e.ClassId == 1);
            
            // check data
            Assert.Equal("A", enrollment.Grade);
        }

        [Fact]
        public void Test_LetterGrades()
        {
            var profCtrl = new ProfessorController(MakeTinyDB());
            MethodInfo convertMethod = typeof(ProfessorController).GetMethod("ConvertPercentageToLetter", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal("A", convertMethod.Invoke(profCtrl, new object[] { 92.0 }));
            Assert.Equal("A-", convertMethod.Invoke(profCtrl, new object[] { 87.0 }));
            Assert.Equal("B+", convertMethod.Invoke(profCtrl, new object[] { 82.0 }));
            Assert.Equal("B", convertMethod.Invoke(profCtrl, new object[] { 77.0 }));
            Assert.Equal("B-", convertMethod.Invoke(profCtrl, new object[] { 72.0 }));
            Assert.Equal("C+", convertMethod.Invoke(profCtrl, new object[] { 67.0 }));
            Assert.Equal("C", convertMethod.Invoke(profCtrl, new object[] { 62.0 }));
            Assert.Equal("C-", convertMethod.Invoke(profCtrl, new object[] { 57.0 }));
            Assert.Equal("D", convertMethod.Invoke(profCtrl, new object[] { 52.0 }));
            Assert.Equal("F", convertMethod.Invoke(profCtrl, new object[] { 48.0 }));
        }

        LMSContext MakeTinyDB()
        {
            var options = new DbContextOptionsBuilder<LMSContext>()
                .UseInMemoryDatabase("LMSControllerTest")
                .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .UseApplicationServiceProvider(NewServiceProvider())
                .Options;

            var db = new LMSContext(options);
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            var dept = new Department { DeptId = 1, Name = "Computer Science", SubjectAbbr = "CS" };
            db.Departments.Add(dept);

            db.Professors.Add(new Professor 
            { 
                UId = "u0000002", 
                FName = "Daniel", 
                LName = "Kopta", 
                Dob = new DateOnly(2000, 1, 1), 
                DepartmentId = 1
            });

            db.Students.Add(new Student 
            { 
                UId = "u0000003", 
                FName = "Landon", 
                LName = "West", 
                Dob = new DateOnly(2000, 1, 1), 
                Major = "CS"
            });

            var course = new Course { CourseId = 1, DeptId = dept.DeptId, Name = "Database Systems", Number = 5530 };
            db.Courses.Add(course);

            var cls = new Class
            {
                ClassId = 1,
                CourseId = course.CourseId,
                ProfessorId = "u0000002",
                SemesterSeason = "Fall",
                SemesterYear = 2025,
                Location = "WEB",
                StartTime = TimeOnly.Parse("09:00:00"),
                EndTime = TimeOnly.Parse("10:20:00")
            };
            db.Classes.Add(cls);

            var enrollment = new Enrollment { StudentId = "u0000003", ClassId = cls.ClassId, Grade = "--" };
            db.Enrollments.Add(enrollment);

            var catA = new AssignmentCategory { CategoryId = 1, ClassId = cls.ClassId, Name = "Homework", Weight = 50 };
            db.AssignmentCategories.Add(catA);

            var asgA = new Assignment
            {
                AssignmentId = 1,
                CategoryId = catA.CategoryId,
                Name = "HW1",
                MaxPoints = 100,
                Due = DateTime.Today.AddDays(14),
                Contents = "This is Homework 1"
            };
            db.Assignments.Add(asgA);

            var catB = new AssignmentCategory { CategoryId = 2, ClassId = cls.ClassId, Name = "Exams", Weight = 40 };
            db.AssignmentCategories.Add(catB);

            db.SaveChanges();
            return db;
        }

        private static ServiceProvider NewServiceProvider()
        {
            return new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();
        }
    }
}
