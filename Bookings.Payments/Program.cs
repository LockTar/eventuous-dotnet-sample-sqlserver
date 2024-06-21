using Bookings.Infrastructure;
using Bookings.Payments;
using Bookings.Payments.Domain;
using Eventuous;
using Eventuous.SqlServer;
using Serilog;

TypeMap.RegisterKnownEventTypes();
Logging.ConfigureLog();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// OpenTelemetry instrumentation must be added before adding Eventuous services
builder.Services.AddTelemetry();
builder.Services.AddEventuous(builder.Configuration);

var app = builder.Build();

app.UseEventuousLogs();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();

// Here we discover commands by their annotations
app.MapDiscoveredCommands<PaymentState>();

if (app.Configuration.GetValue<bool>("SqlServer:InitializeDatabase"))
{
    await InitialiseSchema(app);
}

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