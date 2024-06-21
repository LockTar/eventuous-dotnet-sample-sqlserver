using Bookings;
using Bookings.Domain.Bookings;
using Bookings.Infrastructure;
using Eventuous;
using Eventuous.AspNetCore;
using Eventuous.Diagnostics.Logging;
using Eventuous.Spyglass;
using Eventuous.SqlServer;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Configuration;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Serilog;
using Serilog.Events;

TypeMap.RegisterKnownEventTypes(typeof(BookingEvents.V1.RoomBooked).Assembly);
Logging.ConfigureLog();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Trace).AddConsole();
builder.Host.UseSerilog();

builder.Services
    .AddControllers()
    .AddJsonOptions(cfg => cfg.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddTelemetry();
builder.Services.AddEventuous(builder.Configuration);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("SqlServer:InitializeDatabase"))
{
    await InitialiseSchema(app);
}

app.UseSerilogRequestLogging();
app.UseEventuousLogs();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

try
{
    app.Run();
    //app.Run("http://*:5051");
    return 0;
}
catch (Exception e)
{
    Log.Fatal(e, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

async Task InitialiseSchema(WebApplication app)
{
    var store = app.Services.GetRequiredService<SqlServerStore>();
    var schema = store.Schema;

    string? connectionString = app.Configuration.GetValue<string>("SqlServer:ConnectionString");

    if (connectionString == null)
        throw new InvalidOperationException("Setting SqlServer:ConnectionString is not set");

    await schema.CreateSchema(connectionString, app.Services.GetRequiredService<ILogger<Schema>>(), default);
}
