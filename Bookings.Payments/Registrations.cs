using Bookings.Payments.Application;
using Bookings.Payments.Domain;
using Bookings.Payments.Integration;
using Eventuous.RabbitMq.Producers;
using Eventuous.SqlServer;
using Eventuous.SqlServer.Subscriptions;

namespace Bookings.Payments;

public static class Registrations
{
    public static void AddEventuous(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("default")!;

        // Add Eventuous on SQL Server
        string schemaName = Eventuous.SqlServer.Schema.DefaultSchema;
        services.AddSingleton(new SubscriptionSchemaInfo(schemaName));
        services.AddEventuousSqlServer(connectionString, schema: schemaName, initializeDatabase: true);
        services.AddAggregateStore<SqlServerStore>();
        services.AddSingleton(new SqlServerCheckpointStoreOptions { Schema = schemaName, ConnectionString = connectionString });
        services.AddCheckpointStore<SqlServerCheckpointStore>();

        services.AddCommandService<CommandService, PaymentState>();

        services.AddProducer<RabbitMqProducer>();

        services
            .AddGateway<SqlServerAllStreamSubscription, SqlServerAllStreamSubscriptionOptions, RabbitMqProducer, RabbitMqProduceOptions>(
                subscriptionId: "IntegrationSubscription",
                routeAndTransform: PaymentsGateway.Transform
            );
    }
}

public record SubscriptionSchemaInfo(string Schema);