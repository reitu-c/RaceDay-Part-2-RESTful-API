using System;
namespace RaceDay.Part2.Models;

public class Enrolment
{
    public int EnrolmentID { get; set; }

    public int ParticipantID { get; set; }

    public int CategoryID { get; set; }

    public DateTime EnrolmentDate { get; set; }

    public string? EmergencyContact { get; set; }

    public string Status { get; set; } = "Confirmed";
}

