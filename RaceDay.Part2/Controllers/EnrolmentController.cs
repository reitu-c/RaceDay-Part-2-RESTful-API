using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Part2.Authorization;
using RaceDay.Part2.Data;
using RaceDay.Part2.DTOs;
using RaceDay.Part2.Models;

namespace RaceDay.Part2.Controllers;

[ApiController]
[Route("api/[controller]")]
[SessionAuthorization("Participant")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public EnrolmentsController(RaceDayContext context)
    {
        _context = context;
    }

    //GET: api/Enrolments
    //A participant can view only their own enrolments
    [HttpGet]
    public async Task<IActionResult> GetMyEnrolments()
    {
        int participantId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var enrolments = await _context.Enrolments
            .AsNoTracking()
            .Where(e => e.ParticipantID == participantId)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments);
    }

    //GET: api/Enrolments/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetMyEnrolment(int id)
    {
        int participantId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var enrolment = await _context.Enrolments
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.EnrolmentID == id &&
                e.ParticipantID == participantId);

        if (enrolment == null)
        {
            return NotFound(new
            {
                message = "Enrolment not found."
            });
        }

        return Ok(enrolment);
    }

    //POST: api/Enrolments
    [HttpPost]
    public async Task<IActionResult> CreateEnrolment(
        [FromBody] EnrolmentCreateRequest request)
    {
        int participantId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.CategoryID == request.CategoryID);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e =>
                e.EventID == category.EventID);

        if (raceEvent == null)
        {
            return NotFound(new
            {
                message = "The event associated with this category was not found."
            });
        }

        if (raceEvent.Status != "Open")
        {
            return Conflict(new
            {
                message = "This event is not open for enrolments."
            });
        }

        if (raceEvent.EventDate.Date < DateTime.Today)
        {
            return Conflict(new
            {
                message = "You cannot enrol in an event that has already taken place."
            });
        }

        var existingEnrolment = await _context.Enrolments
            .FirstOrDefaultAsync(e =>
                e.ParticipantID == participantId &&
                e.CategoryID == request.CategoryID);

        if (existingEnrolment != null &&
            existingEnrolment.Status == "Confirmed")
        {
            return Conflict(new
            {
                message = "You are already enrolled in this category."
            });
        }

        //Check the capacity before creating or reactivating an enrolment
        if (category.MaxParticipants.HasValue)
        {
            int confirmedCount = await _context.Enrolments
                .CountAsync(e =>
                    e.CategoryID == request.CategoryID &&
                    e.Status == "Confirmed");

            if (confirmedCount >= category.MaxParticipants.Value)
            {
                return Conflict(new
                {
                    message = "This category has reached its participant limit."
                });
            }
        }

        if (existingEnrolment != null)
        {
            //Reactivate the existing row because the database
            //has a unique constraint on ParticipantID + CategoryID
            existingEnrolment.Status = "Confirmed";
            existingEnrolment.EnrolmentDate = DateTime.Now;
            existingEnrolment.EmergencyContact =
                request.EmergencyContact?.Trim();

            await _context.SaveChangesAsync();

            return Ok(existingEnrolment);
        }

        var enrolment = new Enrolment
        {
            ParticipantID = participantId,
            CategoryID = request.CategoryID,
            EnrolmentDate = DateTime.Now,
            EmergencyContact = request.EmergencyContact?.Trim(),
            Status = "Confirmed"
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetMyEnrolment),
            new { id = enrolment.EnrolmentID },
            enrolment);
    }

    // DELETE: api/Enrolments/5
    // Cancels the enrolment without deleting its database record.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        int participantId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var enrolment = await _context.Enrolments
            .FirstOrDefaultAsync(e =>
                e.EnrolmentID == id &&
                e.ParticipantID == participantId);

        if (enrolment == null)
        {
            return NotFound(new
            {
                message = "Enrolment not found."
            });
        }

        if (enrolment.Status == "Cancelled")
        {
            return Conflict(new
            {
                message = "This enrolment has already been cancelled."
            });
        }

        enrolment.Status = "Cancelled";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Enrolment cancelled successfully.",
            enrolment.EnrolmentID,
            enrolment.Status
        });
    }
}

