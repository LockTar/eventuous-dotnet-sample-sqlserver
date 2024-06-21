using Bookings.Application.Queries;
using Eventuous;
using Eventuous.SqlServer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NodaTime;
using System.Data;

namespace Bookings.HttpApi.Bookings;

[Route("/projections/sql/bookings")]
[ApiController]
public class BookingsProjectionsQueryApi : ControllerBase
{
    private readonly IAggregateStore _store;
    private readonly string _connectionString;
    private readonly SubscriptionSchemaInfo _schemaInfo;

    public BookingsProjectionsQueryApi(IAggregateStore store, SubscriptionOptions subscriptionOptions, SubscriptionSchemaInfo schemaInfo)
    {
        _store = store;
        _schemaInfo = schemaInfo;

        if (subscriptionOptions.ConnectionString is null)
        {
            throw new ArgumentNullException(nameof(subscriptionOptions.ConnectionString));
        }
        _connectionString = subscriptionOptions.ConnectionString;
    }

    [HttpGet]
    public async Task<IActionResult> GetBooking(CancellationToken cancellationToken)
    {
        await using var connection = await ConnectionFactory.GetConnection(_connectionString, cancellationToken);

        var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT * FROM {_schemaInfo.Schema}.bookings";
        cmd.CommandType = CommandType.Text;

        using (SqlDataReader reader = cmd.ExecuteReader())
        {
            List<BookingDocument> bookings = new();

            while (reader.Read())
            {
                bookings.Add(new BookingDocument
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

            return Ok(bookings);
        }
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetBooking(string id, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectionFactory.GetConnection(_connectionString, cancellationToken);

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