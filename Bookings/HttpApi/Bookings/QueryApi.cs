using Bookings.Application.Queries;
using Bookings.Domain.Bookings;
using Eventuous;
using Eventuous.SqlServer;
using Eventuous.SqlServer.vNext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NodaTime;
using System.Data;

namespace Bookings.HttpApi.Bookings;

[Route("/bookings")]
[ApiController]
public class QueryApi : ControllerBase {
    private readonly IAggregateStore _store;
        
    public QueryApi(IAggregateStore store) => _store = store;

    [HttpGet]
    [Route("{id}")]
    public async Task<BookingState> GetBooking(string id, CancellationToken cancellationToken) {
        var booking = await _store.Load<Booking>(StreamName.For<Booking>(id), cancellationToken);
        return booking.State;
    }
}

[Route("/projections/sql/bookings")]
[ApiController]
public class ProjectionsQueryApi : ControllerBase
{
    private readonly IAggregateStore _store;
    private readonly SubscriptionConnectionInfo _connectionInfo;
    private readonly SubscriptionSchemaInfo _schemaInfo;

    public ProjectionsQueryApi(IAggregateStore store, SubscriptionConnectionInfo connectionInfo, SubscriptionSchemaInfo schemaInfo)
    {
        _store = store;
        _connectionInfo = connectionInfo;
        _schemaInfo = schemaInfo;
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetBooking(string id, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectionFactory.GetConnection(_connectionInfo.ConnectionString, cancellationToken);
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT * FROM {_schemaInfo.Schema}.bookings WHERE id = @booking_id;";
        cmd.Parameters.Add(new SqlParameter("@booking_id", id));
        cmd.CommandType = CommandType.Text;

        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            if (reader.Read())
            {
                return Ok(new BookingDocument
                {
                    Id = reader["Id"].ToString(),
                    GuestId = reader["GuestId"].ToString(),
                    RoomId = reader["RoomId"].ToString(),
                    CheckInDate = LocalDate.FromDateTime((DateTime)reader["CheckInDate"]),
                    CheckOutDate = LocalDate.FromDateTime((DateTime)reader["CheckOutDate"]),
                    BookingPrice = float.Parse(reader["BookingPrice"].ToString()),
                    PaidAmount = float.Parse(reader["PaidAmount"].ToString()),
                    Outstanding = float.Parse(reader["Outstanding"].ToString()),
                    Paid = bool.Parse(reader["Paid"].ToString())
                });
            }
            else
            {
                return NotFound();
            }
        }
    }
}