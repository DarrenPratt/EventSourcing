using Marten;
using Marten.Events.Aggregation;
using Marten.Events.Projections;
using EventSourcing.Models;

namespace EventSourcing.Projections
{
    // Handler for projecting student-related events into a StudentProjection
    public class StudentProjectionHandler : SingleStreamProjection<StudentProjection>
    {
        // Constructor to set the custom projection name
        public StudentProjectionHandler()
        {
            ProjectionName = "student_projection"; // Optional: Custom name for the projection
        }

        // Method to create a new StudentProjection from a StudentCreated event
        public StudentProjection Create(StudentCreated @event)
        {
            return new StudentProjection
            {
                Id = @event.StudentId,
                FullName = @event.FullName,
                Email = @event.Email,
                DateOfBirth = @event.DateOfBirth,
                EnrolledCourses = new List<string>()
            };
        }

        // Method to apply changes from a StudentUpdated event to an existing StudentProjection
        public void Apply(StudentUpdated @event, StudentProjection student)
        {
            student.FullName = @event.FullName;
            student.Email = @event.Email;
        }

        // Method to apply changes from a StudentEnrolled event to an existing StudentProjection
        public void Apply(StudentEnrolled @event, StudentProjection student)
        {
            if (!student.EnrolledCourses.Contains(@event.CourseName))
                student.EnrolledCourses.Add(@event.CourseName);
        }

        // Method to apply changes from a StudentUnenrolled event to an existing StudentProjection
        public void Apply(StudentUnenrolled @event, StudentProjection student)
        {
            student.EnrolledCourses.Remove(@event.CourseName);
        }
    }
}
