using System;
namespace RaceDay.Part2.Models;

public class Event
{
    public int EventID { get; set; }

    public int OrganiserID { get; set; }

    public string EventName { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public string Location { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Status { get; set; } = "Open";
}

