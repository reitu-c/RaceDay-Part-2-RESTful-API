using System;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.Part2.DTOs;

public class ResultCreateRequest
{
    [Range(1, int.MaxValue)]
    public int EnrolmentID { get; set; }

    public TimeSpan? FinishTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? OverallPosition { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryPosition { get; set; }

    [Required]
    [RegularExpression(
        "^(Finished|Did Not Finish|Disqualified)$",
        ErrorMessage = "Result status must be Finished, Did Not Finish, or Disqualified.")]
    public string ResultStatus { get; set; } = "Finished";
}
