using System;
namespace RaceDay.Part2.Models;

public class Result
{
    public int ResultID { get; set; }

    public int EnrolmentID { get; set; }

    public TimeSpan? FinishTime { get; set; }

    public int? OverallPosition { get; set; }

    public int? CategoryPosition { get; set; }

    public string ResultStatus { get; set; } = "Finished";
}

