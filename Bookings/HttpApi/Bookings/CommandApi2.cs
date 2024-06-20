using Bookings.Domain;
using Bookings.Domain.Bookings;
using Eventuous;
using Eventuous.AspNetCore.Web;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Net;
using static Bookings.Application.BookingCommands;

namespace Bookings.HttpApi.Bookings;

/// <summary>
/// This command API is for demo purposes only. It's the same as the CommandApi, but with a different route and a custom result type.
/// Check the swagger UI for the API documentation and see the custom result type in action.
/// </summary>
[Route("/booking2")]
public class CommandApi2 : CommandHttpApiBase<Booking, BookingResult>
{
    public CommandApi2(ICommandService<Booking> service) : base(service) { }

    [HttpPost]
    [Route("book")]
    [SwaggerResponse((int)HttpStatusCode.OK, "OK", typeof(MyCustomBookingResult))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Bad Request", typeof(ValidationProblemDetails))]
    public async Task<ActionResult<MyCustomBookingResult>> BookRoom([FromBody] BookRoom cmd, CancellationToken cancellationToken)
    {
        var result = await Handle(cmd, cancellationToken);

        if (result.Value is not BookingResult bookingResult)
        {
            return BadRequest();
        }
        else
        {
            return Ok(new MyCustomBookingResult
            {
                GuestId = bookingResult.State!.GuestId,
                RoomId = bookingResult.State!.RoomId,
                
                CustomPropertyA = "Some custom property",
                CustomPropertyB = "Another custom property",
                CustomPropertyC = "Yet another custom property"
            });
        }
    }
}

public record BookingResult : Result
{
    public new BookingState? State { get; init; }
}

public record MyCustomBookingResult
{
    public string GuestId { get; init; } = null!;
    public RoomId RoomId { get; init; } = null!;

    public string? CustomPropertyA { get; init; }
    public string? CustomPropertyB { get; init; }
    public string? CustomPropertyC { get; init; }
}