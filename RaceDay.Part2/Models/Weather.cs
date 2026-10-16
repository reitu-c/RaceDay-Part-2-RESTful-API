using System;
namespace RaceDay.Part2.Models;

public class Weather
{
    public int WeatherID { get; set; }

    public int EventID { get; set; }

    public DateTime RecordedAt { get; set; }

    public decimal? TemperatureC { get; set; }

    public string? Condition { get; set; }

    public decimal? WindSpeedKmh { get; set; }

    public decimal? RainProbability { get; set; }
}

