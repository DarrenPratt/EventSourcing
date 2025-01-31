namespace EventSourcing.Models
{
    public record StudentCreated(Guid StudentId, string FullName, string Email, DateTime DateOfBirth);
    public record StudentUpdated(Guid StudentId, string FullName, string Email);
    public record StudentEnrolled(Guid StudentId, string CourseName);
    public record StudentUnenrolled(Guid StudentId, string CourseName);
}
