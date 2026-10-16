using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part2.DTOs;

public class RouteUpdateRequest
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string RouteName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal DistanceKM { get; set; }

    [Required]
    [StringLength(150)]
    public string StartPoint { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string FinishPoint { get; set; } = string.Empty;

    [Url]
    [StringLength(500)]
    public string? RouteURL { get; set; }
}
