# 🚀 PostgreSQL + Marten Event Sourcing Sample

This guide provides step-by-step instructions on setting up **PostgreSQL with Marten** for event sourcing in an ASP.NET Core application.

---

## **📌 Prerequisites**
Ensure you have the following installed:
- **.NET 7 or later** ([Download](https://dotnet.microsoft.com/download))
- **Docker & Docker Compose** ([Download](https://www.docker.com/get-started))
- **PostgreSQL Client** (Optional, for database management)

---

## **1️⃣ Step 1: Create a New ASP.NET Core Web API Project**
```sh
dotnet new webapi -n MartenEventSourcing
cd MartenEventSourcing
```

---

## **2️⃣ Step 2: Install Required NuGet Packages**
Install **Marten**, **Weasel Core**, and **PostgreSQL driver**:
```sh
dotnet add package Marten
dotnet add package Weasel.Core
```

---

## **3️⃣ Step 3: Set Up PostgreSQL with Docker**
Create a `docker-compose.yml` file in your project root:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:15
    container_name: postgres_eventstore
    restart: always
    environment:
      POSTGRES_DB: eventstore
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: Secret!
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    networks:
      - event_network

volumes:
  postgres_data:

networks:
  event_network:
    driver: bridge
```

Start PostgreSQL:
```sh
docker-compose up -d
```
Verify the database is running:
```sh
docker ps
```

---

## **4️⃣ Step 4: Configure PostgreSQL Connection**
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "EventStoreDb": "Host=localhost;Database=eventstore;Username=postgres;Password=Secret!"
  }
}
```

---

## **5️⃣ Step 5: Configure Marten in `Program.cs`**
Modify `Program.cs` to register **Marten**:

```csharp
using Marten;
using Weasel.Core;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("EventStoreDb")
    ?? throw new InvalidOperationException("Database connection string is missing!");

builder.Services.AddMarten(opts =>
{
    opts.Connection(connectionString);
    opts.AutoCreateSchemaObjects = AutoCreate.All;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.Run();
```

---

## **6️⃣ Step 6: Define Event Models**
Create a new file `Models/Events.cs`:
```csharp
namespace EventSourcingMarten.Models
{
    public record StudentCreated(Guid StudentId, string FullName, string Email, DateTime DateOfBirth);
    public record StudentUpdated(Guid StudentId, string FullName, string Email);
    public record StudentEnrolled(Guid StudentId, string CourseName);
    public record StudentUnenrolled(Guid StudentId, string CourseName);
}
```

---

## **7️⃣ Step 7: Define Aggregate (`Student.cs`)**
Create `Models/Student.cs`:
```csharp
using Marten.Events.Aggregation;

namespace EventSourcingMarten.Models
{
    public class Student
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> EnrolledCourses { get; set; } = new();
        public DateTime DateOfBirth { get; set; }

        public void Apply(StudentCreated @event)
        {
            Id = @event.StudentId;
            FullName = @event.FullName;
            Email = @event.Email;
            DateOfBirth = @event.DateOfBirth;
        }

        public void Apply(StudentUpdated @event)
        {
            FullName = @event.FullName;
            Email = @event.Email;
        }

        public void Apply(StudentEnrolled @event)
        {
            if (!EnrolledCourses.Contains(@event.CourseName))
                EnrolledCourses.Add(@event.CourseName);
        }

        public void Apply(StudentUnenrolled @event)
        {
            EnrolledCourses.Remove(@event.CourseName);
        }
    }
}
```

8️⃣ Step **8: Define Event Store Service (Services/StudentEventStore.cs)**

Create Services/StudentEventStore.cs:
```
csharp
using Marten;
using EventSourcingTutorial.Models;

namespace EventSourcingTutorial.Services
{
    public class StudentEventStore
    {
        private readonly IDocumentStore _store;

        public StudentEventStore(IDocumentStore store)
        {
            _store = store;
        }

        public async Task SaveEventAsync<T>(Guid streamId, T @event) where T : class
        {
            using var session = _store.LightweightSession();
            session.Events.Append(streamId, @event);
            await session.SaveChangesAsync();
        }

        public async Task<Student?> GetStudentAsync(Guid studentId)
        {
            using var session = _store.QuerySession();
            return await session.Events.AggregateStreamAsync<Student>(studentId);
        }
    }
}
```

## **9️⃣ Step 9: Define Aggregate (`Services/StudentEventStore.cs`)**
```csharp
using Microsoft.AspNetCore.Mvc;
using EventSourcingTutorial.Services;
using EventSourcingTutorial.Models;

namespace EventSourcingTutorial.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentController : ControllerBase
    {
        private readonly StudentEventStore _eventStore;

        public StudentController(StudentEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateStudent([FromBody] StudentCreated studentCreated)
        {
            await _eventStore.SaveEventAsync(studentCreated.StudentId, studentCreated);
            return Ok(new { Message = "Student created", studentCreated.StudentId });
        }

        [HttpPost("enroll")]
        public async Task<IActionResult> EnrollStudent([FromBody] StudentEnrolled studentEnrolled)
        {
            await _eventStore.SaveEventAsync(studentEnrolled.StudentId, studentEnrolled);
            return Ok(new { Message = "Student enrolled", studentEnrolled.StudentId });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateStudent([FromBody] StudentUpdated studentUpdated)
        {
            await _eventStore.SaveEventAsync(studentUpdated.StudentId, studentUpdated);
            return Ok(new { Message = "Student updated", studentUpdated.StudentId });
        }

        [HttpGet("{studentId}")]
        public async Task<IActionResult> GetStudent(Guid studentId)
        {
            var student = await _eventStore.GetStudentAsync(studentId);
            if (student == null) return NotFound();

            return Ok(student);
        }
    }
}
```



---

## **🔟 Step 10: Add Unenrollment to Controller (`StudentController.cs`)**
Modify `Controllers/StudentController.cs`:
```csharp
[HttpPost("unenroll")]
public async Task<IActionResult> UnenrollStudent([FromBody] StudentUnenrolled studentUnenrolled)
{
    await _eventStore.SaveEventAsync(studentUnenrolled.StudentId, studentUnenrolled);
    return Ok(new { Message = "Student unenrolled", studentUnenrolled.StudentId });
}
```

---

## **11 Step 10: Run & Test**
Start the API:
```sh
dotnet run
```
Open **Swagger UI**:
```
http://localhost:5000/swagger
```

📌 **Now you have a working PostgreSQL + Marten Event Sourcing setup with student unenrollment!** 🚀

---

## **🧪 Tests**
### **Create Student**
```json
{
  "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fullName": "Ada Lovelace",
  "email": "ada@loclahost.co.uk",
  "dateOfBirth": "1815-12-10T00:00:00.000Z"
}
```

### **Update Student**
```json
{
  "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fullName": "Ada Lovelace",
  "email": "ada.Lovelace@loclahost.com"
}
```

### **Enroll in Course**
```json
{
  "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "courseName": "Event Sourcing 101"
}
```

```json
{
  "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "courseName": "The Big Course"
}
```

### **Unenroll from Course**
```json
{
  "studentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "courseName": "The Big Course"
}
```


---

# 🚀 Adding Projections
📌 What is a Projection in Event Sourcing?
A projection is a read model that represents aggregated or transformed event data in a queryable format. Instead of querying raw events, a projection provides a precomputed view of data, making it easier to query without reapplying all events.

In Marten, projections are used to automatically derive state from event streams.


## **1️⃣ Step 1: Update Marten in `Program.cs`**
Modify `Program.cs` to register **Marten** and enable asynchronous projections:

```csharp
using Marten;
using Weasel.Core;
using EventSourcingMarten.Projections;

// Register Marten with PostgreSQL and enable Async Daemon
builder.Services.AddMarten(opts =>
{
    opts.Connection(connectionString);
    opts.AutoCreateSchemaObjects = AutoCreate.All;

    // ✅ Register Asynchronous Projection
    opts.Projections.Add<StudentProjectionHandler>(ProjectionLifecycle.Async);

    // Register event types
    opts.Events.AddEventTypes(new[]
    {
        typeof(StudentCreated),
        typeof(StudentUpdated),
        typeof(StudentEnrolled),
        typeof(StudentUnenrolled)
    });
})
// ✅ Register the built-in Marten Daemon
.AddAsyncDaemon(DaemonMode.Solo);
```

---

## **2️⃣ Step 2: Define Projection (`StudentProjectionHandler.cs`)**
Create `Projections/StudentProjectionHandler.cs`:
```csharp
using Marten;
using Marten.Events.Aggregation;
using EventSourcingMarten.Models;

namespace EventSourcingMarten.Projections
{
    public class StudentProjectionHandler : SingleStreamAggregation<StudentProjection>
    {
        public StudentProjection Create(StudentCreated @event)
        {
            return new StudentProjection
            {
                Id = @event.StudentId,
                FullName = @event.FullName,
                Email = @event.Email,
                DateOfBirth = @event.DateOfBirth
            };
        }

        public void Apply(StudentUpdated @event, StudentProjection student)
        {
            student.FullName = @event.FullName;
            student.Email = @event.Email;
        }

        public void Apply(StudentEnrolled @event, StudentProjection student)
        {
            if (!student.EnrolledCourses.Contains(@event.CourseName))
                student.EnrolledCourses.Add(@event.CourseName);
        }

        public void Apply(StudentUnenrolled @event, StudentProjection student)
        {
            student.EnrolledCourses.Remove(@event.CourseName);
        }
    }
}
```

---

## **3️⃣ Step 3: Query Projections via API**
Modify `Controllers/StudentController.cs`:
```csharp
[HttpGet("projection/{studentId}")]
public async Task<IActionResult> GetStudentProjection(Guid studentId, [FromServices] IDocumentSession session)
{
    var studentProjection = await session.LoadAsync<StudentProjection>(studentId);
    if (studentProjection == null) return NotFound();

    return Ok(studentProjection);
}
```

---

## **4️⃣ Step 4: Run & Test**
Start the API:
```sh
dotnet run apply-migrations
dotnet run rebuild-projections
```
Open **Swagger UI**:
```
http://localhost:5000/swagger
```

📌 **Now you have a working PostgreSQL + Marten Event Sourcing setup with projections!** 🚀

---

