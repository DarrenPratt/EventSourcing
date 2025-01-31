using Microsoft.AspNetCore.Mvc;
using EventSourcing.Services;
using EventSourcing.Models;
using Marten;

namespace EventSourcing.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentController : ControllerBase
    {
        private readonly StudentEventStore _eventStore;

        // Constructor to initialize the StudentController with a StudentEventStore instance
        public StudentController(StudentEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        // Endpoint to create a new student
        [HttpPost("create")]
        public async Task<IActionResult> CreateStudent([FromBody] StudentCreated studentCreated)
        {
            await _eventStore.SaveEventAsync(studentCreated.StudentId, studentCreated);
            return Ok(new { Message = "Student created", studentCreated.StudentId });
        }

        // Endpoint to enroll a student in a course
        [HttpPost("enroll")]
        public async Task<IActionResult> EnrollStudent([FromBody] StudentEnrolled studentEnrolled)
        {
            await _eventStore.SaveEventAsync(studentEnrolled.StudentId, studentEnrolled);
            return Ok(new { Message = "Student enrolled", studentEnrolled.StudentId });
        }

        // Endpoint to unenroll a student from a course
        [HttpPost("unenroll")]
        public async Task<IActionResult> UnenrollStudent([FromBody] StudentUnenrolled studentUnenrolled)
        {
            await _eventStore.SaveEventAsync(studentUnenrolled.StudentId, studentUnenrolled);
            return Ok(new { Message = "Student unenrolled", studentUnenrolled.StudentId });
        }

        // Endpoint to update student information
        [HttpPost("update")]
        public async Task<IActionResult> UpdateStudent([FromBody] StudentUpdated studentUpdated)
        {
            await _eventStore.SaveEventAsync(studentUpdated.StudentId, studentUpdated);
            return Ok(new { Message = "Student updated", studentUpdated.StudentId });
        }

        // Endpoint to get the current state of a student by their ID
        [HttpGet("{studentId}")]
        public async Task<IActionResult> GetStudent(Guid studentId)
        {
            var student = await _eventStore.GetStudentAsync(studentId);
            if (student == null) return NotFound();

            return Ok(student);
        }

        // Endpoint to get the projected state of a student by their ID
        [HttpGet("projection/{studentId}")]
        public async Task<IActionResult> GetStudentProjection(Guid studentId, [FromServices] IDocumentSession session)
        {
            var studentProjection = await session.LoadAsync<StudentProjection>(studentId);
            if (studentProjection == null) return NotFound();

            return Ok(studentProjection);
        }
    }
}
