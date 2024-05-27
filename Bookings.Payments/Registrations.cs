using Bookings.Payments.Application;
using Bookings.Payments.Domain;
using Bookings.Payments.Infrastructure;
using Bookings.Payments.Integration;
using Eventuous.Diagnostics.OpenTelemetry;
using Eventuous.Producers;
using Eventuous.RabbitMq.Producers;
using Eventuous.SqlServer;
using Eventuous.SqlServer.Subscriptions;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bookings.Payments;

public static class Registrations
{
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("default")!;

        // Add Eventuous on SQL Server
        string schemaName = Eventuous.SqlServer.Schema.DefaultSchema;
        services.AddSingleton(new SubscriptionSchemaInfo(schemaName));
        services.AddEventuousSqlServer(connectionString, schema: schemaName, initializeDatabase: true);
        services.AddAggregateStore<SqlServerStore>();
        services.AddSingleton(new SqlServerCheckpointStoreOptions { Schema = schemaName, ConnectionString = connectionString });
        services.AddCheckpointStore<SqlServerCheckpointStore>();

        services.AddCommandService<CommandService, Payment>();

        services.AddProducer<RabbitMqProducer>();
        
        services
            .AddGateway<SqlServerAllStreamSubscription, SqlServerAllStreamSubscriptionOptions, RabbitMqProducer, RabbitMqProduceOptions>(
                subscriptionId: "IntegrationSubscription",
                routeAndTransform: PaymentsGateway.Transform
            );
    }

    public static void AddTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithMetrics(
                builder => builder
                    .AddAspNetCoreInstrumentation()
                    .AddEventuous()
                    .AddEventuousSubscriptions()
                    .AddPrometheusExporter()
            );

        services.AddOpenTelemetry()
            .WithTracing(
                builder => builder
                    .AddAspNetCoreInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddEventuousTracing()
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("payments"))
                    .SetSampler(new AlwaysOnSampler())
                    .AddZipkinExporter()
            );
    }
}

public record SubscriptionSchemaInfo(string Schema);