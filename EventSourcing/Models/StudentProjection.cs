namespace EventSourcing.Models
{
    public class StudentProjection
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> EnrolledCourses { get; set; } = new();
        public DateTime DateOfBirth { get; set; }
    }
}
