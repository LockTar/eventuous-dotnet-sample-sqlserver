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
[Route("/booking3")]
public class CommandApi3 : CommandHttpApiBase<Booking, BookingResult2>
{
    public CommandApi3(ICommandService<Booking> service) : base(service) { }

    [HttpPost]
    [Route("book")]
    [SwaggerResponse((int)HttpStatusCode.OK, "OK", typeof(MyCustomBookingResult2))]
    [SwaggerResponse((int)HttpStatusCode.BadRequest, "Bad Request", typeof(ValidationProblemDetails))]
    public Task<ActionResult<BookingResult2>> BookRoom([FromBody] BookRoom cmd, CancellationToken cancellationToken)
        => Handle(cmd, cancellationToken);

    protected override ActionResult AsActionResult(Result result)
    {
        if (result is Eventuous.ErrorResult error)
        {
            return error.Exception switch
            {
                FluentValidation.ValidationException => MapValidationExceptionAsValidationProblemDetails(error),
                _ => base.AsActionResult(result)
            };
        }
        else if(result is BookingResult2 okResult)
        {
            if (okResult.State is null)
            {
                return base.AsActionResult(result);
            }

            return Ok(new MyCustomBookingResult2
            {
                GuestId = okResult.State.GuestId,
                RoomId = okResult.State.RoomId,
                
                CustomPropertyA = "Some custom property",
                CustomPropertyB = "Another custom property",
                CustomPropertyC = "Yet another custom property"
            });
        }
        else
        {
            return base.AsActionResult(result);
        }
    }

    private BadRequestObjectResult MapValidationExceptionAsValidationProblemDetails(ErrorResult error)
    {
        if (error is null || error.Exception is null || error.Exception is not FluentValidation.ValidationException)
        {
            throw new ArgumentNullException(nameof(error), "Exception in result is not of the type `ValidationException`. Unable to map validation result.");
        }

        FluentValidation.ValidationException exception = (FluentValidation.ValidationException)error.Exception;

        var problemDetails = new ValidationProblemDetails()
        {
            Status = StatusCodes.Status400BadRequest,
            Detail = "Please refer to the errors property for additional details."
        };

        var groupFailures = exception.Errors.GroupBy(v => v.PropertyName);
        foreach (var groupFailure in groupFailures)
        {
            problemDetails.Errors.Add(groupFailure.Key, groupFailure.Select(s => s.ErrorMessage).ToArray());
        }

        return new BadRequestObjectResult(problemDetails);
    }
}

public record BookingResult2 : Result
{
    public new BookingState? State { get; init; }
}

public record MyCustomBookingResult2
{
    public string GuestId { get; init; } = null!;
    public RoomId RoomId { get; init; } = null!;

    public string? CustomPropertyA { get; init; }
    public string? CustomPropertyB { get; init; }
    public string? CustomPropertyC { get; init; }
}