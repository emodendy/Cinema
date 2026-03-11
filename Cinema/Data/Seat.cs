using System;
using System.Collections.Generic;

namespace Cinema.Data;

public partial class Seat
{
    public int Id { get; set; }

    public int? HallId { get; set; }

    public int Number { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Hall? Hall { get; set; }
}
