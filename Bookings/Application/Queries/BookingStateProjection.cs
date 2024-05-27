using Eventuous.SqlServer;
using Eventuous.SqlServer.vNext;
using Eventuous.Subscriptions.Context;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using static Bookings.Domain.Bookings.BookingEvents;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Bookings.Application.Queries;

public class BookingStateProjection : SqlServerProjector
{
    public BookingStateProjection(SubscriptionConnectionInfo connectionInfo, SubscriptionSchemaInfo schemaInfo) : base(connectionInfo)
    {
        var insertBooking = $"insert into {schemaInfo.Schema}.bookings (Id, GuestId, RoomId, CheckInDate, CheckOutDate, BookingPrice, PaidAmount, Outstanding, Paid) " +
            $"values (@booking_id, @guestId, @roomId, @checkInDate, @checkOutDate, @bookingPrice, @paidAmount, @outstanding, @paid)";
        var paymentRecorded = $"UPDATE {schemaInfo.Schema}.bookings SET outstanding = @outstanding WHERE Id = @booking_id;";
        var fullyPaid = $"UPDATE {schemaInfo.Schema}.bookings SET paid = 'true' WHERE Id = @booking_id;";

        On<V1.RoomBooked>(
            (connection, ctx) =>
            {
                return Project(
                    connection,
                    insertBooking,
                    new SqlParameter("@booking_id", ctx.Stream.GetId()),
                    new SqlParameter("@guestId", ctx.Message.GuestId),
                    new SqlParameter("@roomId", ctx.Message.RoomId),
                    new SqlParameter("@checkInDate", ctx.Message.CheckInDate.ToDateTimeUnspecified()),
                    new SqlParameter("@checkOutDate", ctx.Message.CheckOutDate.ToDateTimeUnspecified()),
                    new SqlParameter("@bookingPrice", ctx.Message.BookingPrice),
                    new SqlParameter("@paidAmount", ctx.Message.PrepaidAmount),
                    new SqlParameter("@outstanding", ctx.Message.OutstandingAmount),
                    new SqlParameter("@paid", ctx.Message.OutstandingAmount == 0 ? true : false)
                );
            });

        On<V1.PaymentRecorded>(
            (connection, ctx) =>
            {
                return Project(
                    connection,
                    paymentRecorded,
                    new SqlParameter("@booking_id", ctx.Stream.GetId()),
                    new SqlParameter("@outstanding", ctx.Message.Outstanding)
                );
            });

        On<V1.BookingFullyPaid>(
            (connection, ctx) =>
            {
                return Project(
                    connection,
                    fullyPaid,
                    new SqlParameter("@booking_id", ctx.Stream.GetId())
                );
            });
    }
}
