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
public class WeatherController : ControllerBase
{
    private readonly RaceDayContext _context;

    public WeatherController(RaceDayContext context)
    {
        _context = context;
    }

    // GET: api/Weather
    [HttpGet]
    public async Task<IActionResult> GetWeatherRecords()
    {
        var records = await _context.WeatherRecords
            .AsNoTracking()
            .OrderByDescending(w => w.RecordedAt)
            .ToListAsync();

        return Ok(records);
    }

    // GET: api/Weather/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetWeatherRecord(int id)
    {
        var record = await _context.WeatherRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WeatherID == id);

        if (record == null)
        {
            return NotFound(new
            {
                message = "Weather record not found."
            });
        }

        return Ok(record);
    }

    // GET: api/Weather/event/5
    [HttpGet("event/{eventId:int}")]
    public async Task<IActionResult> GetWeatherByEvent(int eventId)
    {
        bool eventExists = await _context.Events
            .AnyAsync(e => e.EventID == eventId);

        if (!eventExists)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        var records = await _context.WeatherRecords
            .AsNoTracking()
            .Where(w => w.EventID == eventId)
            .OrderByDescending(w => w.RecordedAt)
            .ToListAsync();

        return Ok(records);
    }

    // POST: api/Weather
    [HttpPost]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> CreateWeatherRecord(
        [FromBody] WeatherCreateRequest request)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        if (request.RecordedAt == default)
        {
            return BadRequest(new
            {
                message = "A valid recording date and time is required."
            });
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == request.EventID);

        if (raceEvent == null)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "You can only manage weather records for your own events."
            });
        }

        if (request.WindSpeedKmh < 0)
        {
            return BadRequest(new
            {
                message = "Wind speed cannot be negative."
            });
        }

        if (request.RainProbability < 0 ||
            request.RainProbability > 100)
        {
            return BadRequest(new
            {
                message = "Rain probability must be between 0 and 100."
            });
        }

        var record = new Weather
        {
            EventID = request.EventID,
            RecordedAt = request.RecordedAt,
            TemperatureC = request.TemperatureC,
            Condition = request.Condition?.Trim(),
            WindSpeedKmh = request.WindSpeedKmh,
            RainProbability = request.RainProbability
        };

        _context.WeatherRecords.Add(record);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetWeatherRecord),
            new { id = record.WeatherID },
            record);
    }

    // PUT: api/Weather/5
    [HttpPut("{id:int}")]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> UpdateWeatherRecord(
        int id,
        [FromBody] WeatherUpdateRequest request)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        if (request.RecordedAt == default)
        {
            return BadRequest(new
            {
                message = "A valid recording date and time is required."
            });
        }

        if (request.WindSpeedKmh < 0)
        {
            return BadRequest(new
            {
                message = "Wind speed cannot be negative."
            });
        }

        if (request.RainProbability < 0 ||
            request.RainProbability > 100)
        {
            return BadRequest(new
            {
                message = "Rain probability must be between 0 and 100."
            });
        }

        var record = await _context.WeatherRecords
            .FirstOrDefaultAsync(w => w.WeatherID == id);

        if (record == null)
        {
            return NotFound(new
            {
                message = "Weather record not found."
            });
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == record.EventID);

        if (raceEvent == null)
        {
            return NotFound(new
            {
                message = "The associated event was not found."
            });
        }

        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "You can only manage weather records for your own events."
            });
        }

        record.RecordedAt = request.RecordedAt;
        record.TemperatureC = request.TemperatureC;
        record.Condition = request.Condition?.Trim();
        record.WindSpeedKmh = request.WindSpeedKmh;
        record.RainProbability = request.RainProbability;

        await _context.SaveChangesAsync();

        return Ok(record);
    }

    // DELETE: api/Weather/5
    [HttpDelete("{id:int}")]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> DeleteWeatherRecord(int id)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var record = await _context.WeatherRecords
            .FirstOrDefaultAsync(w => w.WeatherID == id);

        if (record == null)
        {
            return NotFound(new
            {
                message = "Weather record not found."
            });
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == record.EventID);

        if (raceEvent == null)
        {
            return NotFound(new
            {
                message = "The associated event was not found."
            });
        }

        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "You can only manage weather records for your own events."
            });
        }

        _context.WeatherRecords.Remove(record);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Weather record deleted successfully."
        });
    }
}

