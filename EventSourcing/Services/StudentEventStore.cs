using Marten;
using EventSourcing.Models;

namespace EventSourcing.Services
{
    public class StudentEventStore
    {
        private readonly IDocumentStore _store;

        // Constructor to initialize the StudentEventStore with an IDocumentStore instance
        public StudentEventStore(IDocumentStore store)
        {
            _store = store;
        }

        // Method to save an event to the event stream identified by streamId
        public async Task SaveEventAsync<T>(Guid streamId, T @event) where T : class
        {
            using var session = _store.LightweightSession();
            session.Events.Append(streamId, @event); // Append the event to the stream
            await session.SaveChangesAsync(); // Save changes asynchronously
        }

        // Method to retrieve the current state of a student by aggregating events from the event stream
        public async Task<Student?> GetStudentAsync(Guid studentId)
        {
            using var session = _store.QuerySession();
            return await session.Events.AggregateStreamAsync<Student>(studentId); // Aggregate events to get the student state
        }
    }
}
