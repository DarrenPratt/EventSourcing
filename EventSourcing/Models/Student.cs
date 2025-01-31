using EventSourcing.Models;
using Marten.Events.Aggregation;

namespace EventSourcing
{
    public class Student
    {
        // Unique identifier for the student
        public Guid Id { get; set; }

        // Full name of the student
        public string FullName { get; set; }

        // Email address of the student
        public string Email { get; set; }

        // List of courses the student is enrolled in
        public List<string> EnrolledCourses { get; set; } = new();

        // Date of birth of the student
        public DateTime DateOfBirth { get; set; }

        // Apply method to handle StudentCreated event
        public void Apply(StudentCreated @event)
        {
            Id = @event.StudentId;
            FullName = @event.FullName;
            Email = @event.Email;
            DateOfBirth = @event.DateOfBirth;
        }

        // Apply method to handle StudentUpdated event
        public void Apply(StudentUpdated @event)
        {
            FullName = @event.FullName;
            Email = @event.Email;
        }

        // Apply method to handle StudentEnrolled event
        public void Apply(StudentEnrolled @event)
        {
            EnrolledCourses.Add(@event.CourseName);
        }

        // Apply method to handle StudentUnenrolled event
        public void Apply(StudentUnenrolled @event)
        {
            EnrolledCourses.Remove(@event.CourseName);
        }
    }
}
