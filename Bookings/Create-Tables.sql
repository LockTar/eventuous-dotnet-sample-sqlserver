CREATE TABLE [dbo].[Bookings] (
    [Id]            NVARCHAR (50) NOT NULL,
    [GuestId]       NVARCHAR (50) NOT NULL,
    [RoomId]        NVARCHAR (50) NOT NULL,
    [CheckInDate]   DATETIME2 (7) NOT NULL,
    [CheckOutDate]  DATETIME2 (7) NOT NULL,
    [BookingPrice]  MONEY         NOT NULL,
    [PaidAmount]    MONEY         NOT NULL,
    [Outstanding]   MONEY         NOT NULL,
    [Paid]          BIT           NOT NULL,
    CONSTRAINT [PK_bookings] PRIMARY KEY CLUSTERED ([Id] ASC)
);