using NodaTime;

namespace Bookings.Application.Queries;

public record MyBookings
{
    public string Id { get; init; } = null!;

    public List<Booking> Bookings { get; init; } = new();

    public record Booking(string BookingId, LocalDate CheckInDate, LocalDate CheckOutDate, float Price);
}