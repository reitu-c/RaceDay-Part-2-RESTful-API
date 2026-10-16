using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part2.DTOs;

public class EventUpdateRequest
{
    [Required]
    [StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    [Required]
    [StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [RegularExpression("^(Open|Closed|Cancelled)$")]
    public string Status { get; set; } = "Open";
}

