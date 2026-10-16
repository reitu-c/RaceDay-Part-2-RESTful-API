using System;
namespace RaceDay.Part2.Models;

public class RaceRoute
{
    public int RouteID { get; set; }

    public int EventID { get; set; }

    public string RouteName { get; set; } = string.Empty;

    public decimal DistanceKM { get; set; }

    public string StartPoint { get; set; } = string.Empty;

    public string FinishPoint { get; set; } = string.Empty;

    public string? RouteURL { get; set; }
}

