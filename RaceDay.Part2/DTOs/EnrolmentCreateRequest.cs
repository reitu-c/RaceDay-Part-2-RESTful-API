using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part2.DTOs;

public class EnrolmentCreateRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryID { get; set; }

    [StringLength(100)]
    public string? EmergencyContact { get; set; }
}

