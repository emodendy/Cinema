using System;
using System.Collections.Generic;

namespace Cinema.Data;

public partial class Hall
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
