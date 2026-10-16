using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Part2.Authorization;
using RaceDay.Part2.Data;
using RaceDay.Part2.DTOs;
using RaceDay.Part2.Models;

namespace RaceDay.Part2.Controllers;

[ApiController]
[Route("api/[controller]")]
[SessionAuthorization]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public EventsController(RaceDayContext context)
    {
        _context = context;
    }

    //GET: api/Events
    //Both authenticated roles can view events
    [HttpGet]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events
            .AsNoTracking()
            .OrderBy(e => e.EventDate)
            .ToListAsync();

        return Ok(events);
    }

    //GET: api/Events/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EventID == id);

        if (eventItem == null)
        {
            return NotFound(new { message = "Event not found." });
        }

        return Ok(eventItem);
    }

    //POST: api/Events
    //Only Organisers can create events
    [HttpPost]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> CreateEvent(
        EventCreateRequest request)
    {
        if (request.EventDate == default)
        {
            return BadRequest(new
            {
                message = "A valid event date is required."
            });
        }

        if (request.EventDate.Date < DateTime.Today)
        {
            return BadRequest(new
            {
                message = "The event date cannot be in the past."
            });
        }

        if (request.Status != "Open"
            && request.Status != "Closed"
            && request.Status != "Cancelled")
        {
            return BadRequest(new
            {
                message = "Status must be Open, Closed, or Cancelled."
            });
        }

        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var eventItem = new Event
        {
            OrganiserID = organiserId,
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate.Date,
            Location = request.Location.Trim(),
            Description = request.Description?.Trim(),
            Status = request.Status
        };

        _context.Events.Add(eventItem);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEvent),
            new { id = eventItem.EventID },
            eventItem);
    }

    //PUT: api/Events/5
    //An Organiser can update only their own event
    [HttpPut("{id:int}")]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> UpdateEvent(
        int id,
        EventUpdateRequest request)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e =>
                e.EventID == id &&
                e.OrganiserID == organiserId);

        if (eventItem == null)
        {
            return NotFound(new
            {
                message = "Event not found or you do not own it."
            });
        }

        if (request.EventDate == default ||
            request.EventDate.Date < DateTime.Today)
        {
            return BadRequest(new
            {
                message = "Enter a valid event date that is not in the past."
            });
        }

        eventItem.EventName = request.EventName.Trim();
        eventItem.EventDate = request.EventDate.Date;
        eventItem.Location = request.Location.Trim();
        eventItem.Description = request.Description?.Trim();
        eventItem.Status = request.Status;

        await _context.SaveChangesAsync();

        return Ok(eventItem);
    }

    //DELETE: api/Events/5
    //An Organiser can delete only their own event
    [HttpDelete("{id:int}")]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var eventItem = await _context.Events
            .FirstOrDefaultAsync(e =>
                e.EventID == id &&
                e.OrganiserID == organiserId);

        if (eventItem == null)
        {
            return NotFound(new
            {
                message = "Event not found or you do not own it."
            });
        }

        //Do not delete an event that has dependent records
        bool hasCategories = await _context.Categories
            .AnyAsync(c => c.EventID == id);

        bool hasRoutes = await _context.Routes
            .AnyAsync(r => r.EventID == id);

        bool hasWeatherRecords = await _context.WeatherRecords
            .AnyAsync(w => w.EventID == id);

        if (hasCategories || hasRoutes || hasWeatherRecords)
        {
            return Conflict(new
            {
                message = "This event has related records and cannot be deleted."
            });
        }

        _context.Events.Remove(eventItem);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

