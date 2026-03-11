using System;
using System.Collections.Generic;

namespace Cinema.Data;

public partial class Booking
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? SessionId { get; set; }

    public int? SeatId { get; set; }

    public virtual Seat? Seat { get; set; }

    public virtual Session? Session { get; set; }

    public virtual User? User { get; set; }
}
