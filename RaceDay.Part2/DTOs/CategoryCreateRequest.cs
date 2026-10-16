using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part2.DTOs;

public class CategoryCreateRequest
{
    [Required]
    public int EventID { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string CategoryName { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "9999.99")]
    public decimal DistanceKM { get; set; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal EntryFee { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxParticipants { get; set; }
}

