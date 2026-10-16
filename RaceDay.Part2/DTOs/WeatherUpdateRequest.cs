using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part2.DTOs;

public class WeatherUpdateRequest
{
    public DateTime RecordedAt { get; set; }

    public decimal? TemperatureC { get; set; }

    [StringLength(100)]
    public string? Condition { get; set; }

    [Range(typeof(decimal), "0", "999999.99")]
    public decimal? WindSpeedKmh { get; set; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? RainProbability { get; set; }
}
