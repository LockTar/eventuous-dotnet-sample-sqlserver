using System.Text.Json;
using Bookings.Application;
using Bookings.Application.Queries;
using Bookings.Domain;
using Bookings.Domain.Bookings;
using Bookings.Integration;
using Eventuous;
using Eventuous.Diagnostics.OpenTelemetry;
using Eventuous.RabbitMq.Subscriptions;
using Eventuous.SqlServer;
using Eventuous.SqlServer.Subscriptions;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Bookings;

public static class Registrations
{
    public static void AddEventuous(this IServiceCollection services, IConfiguration configuration)
    {
        DefaultEventSerializer.SetDefaultSerializer(
            new DefaultEventSerializer(
                new JsonSerializerOptions(JsonSerializerDefaults.Web).ConfigureForNodaTime(
                    DateTimeZoneProviders.Tzdb
                )
            )
        );

        string? connectionString = configuration.GetValue<string>("SqlServer:ConnectionString");
        bool? initializeDatabase = configuration.GetValue<bool?>("SqlServer:InitializeDatabase");
        string? aggregateStoreSchema = configuration.GetValue<string>("SqlServer:Schema:AggregateStore");
        string? subscriptionsSchema = configuration.GetValue<string>("SqlServer:Schema:Subscriptions");

        if (connectionString == null)
            throw new InvalidOperationException("Setting SqlServer:ConnectionString is not set");

        if (initializeDatabase == null)
            throw new InvalidOperationException("Setting SqlServer:InitializeDatabase is not set");

        if (aggregateStoreSchema == null)
            aggregateStoreSchema = Eventuous.SqlServer.Schema.DefaultSchema;

        if (subscriptionsSchema == null)
            subscriptionsSchema = "dbo";

        // Add Eventuous on SQL Server
        services.AddSingleton(new SubscriptionSchemaInfo(subscriptionsSchema));
        services.AddSingleton(new SubscriptionOptions() { ConnectionString = connectionString });
        services.AddEventuousSqlServer(connectionString, schema: aggregateStoreSchema, initializeDatabase: initializeDatabase.Value);
        services.AddAggregateStore<SqlServerStore>();
        services.AddSingleton(new SqlServerCheckpointStoreOptions { Schema = aggregateStoreSchema, ConnectionString = connectionString });
        services.AddCheckpointStore<SqlServerCheckpointStore>();

        services.AddCommandService<BookingsCommandService, Booking>();

        services.AddSingleton<Services.IsRoomAvailable>((id, period) => new ValueTask<bool>(true));

        services.AddSingleton<Services.ConvertCurrency>((from, currency)
            => new Money(from.Amount * 2, currency)
        );

        // Add subscriptions for projections
        services.AddSubscription<SqlServerAllStreamSubscription, SqlServerAllStreamSubscriptionOptions>(
            "BookingsProjections",
            builder => builder
                .Configure(options =>
                {
                    options.ThrowOnError = true;
                    options.ConnectionString = connectionString;
                })
                .AddEventHandler<BookingStateProjection>()
        //.AddEventHandler<MyBookingsProjection>() // TODO: Implement MyBookingsProjection
        );

        //services.AddSubscription<RabbitMqSubscription, RabbitMqSubscriptionOptions>(
        //    "PaymentIntegration",
        //    builder => builder
        //        .Configure(x => x.Exchange = PaymentsIntegrationHandler.Stream)
        //        .AddEventHandler<PaymentsIntegrationHandler>()
        //);
    }

    public static void AddTelemetry(this IServiceCollection services)
    {
        var otelEnabled = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") != null;

        services.AddOpenTelemetry()
            .WithMetrics(
                builder =>
                {
                    builder
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("bookings"))
                        .AddAspNetCoreInstrumentation()
                        .AddEventuous()
                        .AddEventuousSubscriptions()
                        .AddPrometheusExporter();
                    if (otelEnabled) builder.AddOtlpExporter();
                }
            );

        services.AddOpenTelemetry()
            .WithTracing(
                builder =>
                {
                    builder
                        .AddAspNetCoreInstrumentation()
                        .AddGrpcClientInstrumentation()
                        .AddEventuousTracing()
                        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("bookings"))
                        .SetSampler(new AlwaysOnSampler());

                    if (otelEnabled)
                        builder.AddOtlpExporter();
                    else
                        builder.AddZipkinExporter();
                }
            );
    }
}
