using EventSourcing.Models;
using EventSourcing.Projections;
using EventSourcing.Services;
using Marten;
using Weasel.Core;
using Marten.Events.Daemon.Resiliency;
using Marten.Events.Projections;

var builder = WebApplication.CreateBuilder(args);

// Load connection string from `appsettings.json`
var connectionString = builder.Configuration.GetConnectionString("EventStoreDb")
    ?? throw new InvalidOperationException("Database connection string is missing!");

// Register Marten with PostgreSQL and enable Async Daemon
builder.Services.AddMarten(opts =>
{
    opts.Connection(connectionString);
    opts.AutoCreateSchemaObjects = AutoCreate.All;

    // Register Asynchronous Projection
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
// Register the built-in Marten Daemon
.AddAsyncDaemon(DaemonMode.Solo);

// Register StudentEventStore
builder.Services.AddScoped<StudentEventStore>();

// Add Authentication & Authorization Services
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Add controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply migrations if the command-line argument is present
if (args.Length > 0 && args[0] == "apply-migrations")
{
    using var scope = app.Services.CreateScope();
    var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

    Console.WriteLine("Applying Marten schema migrations to PostgreSQL...");

    try
    {
        await store.Storage.ApplyAllConfiguredChangesToDatabaseAsync();
        Console.WriteLine("Migrations applied successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex.Message}");
    }

    return; // Exit after applying migrations
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
