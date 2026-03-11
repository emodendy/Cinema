using System;
using System.Collections.Generic;

namespace Cinema.Data;

public partial class Session
{
    public int Id { get; set; }

    public int? MovieId { get; set; }

    public int? HallId { get; set; }

    public DateTime StartTime { get; set; }

    public decimal Price { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual Hall? Hall { get; set; }

    public virtual Movie? Movie { get; set; }
}
