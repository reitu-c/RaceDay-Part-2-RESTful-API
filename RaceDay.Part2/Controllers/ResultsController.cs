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
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _context;

    public ResultsController(RaceDayContext context)
    {
        _context = context;
    }

    //GET: api/Results
    //Authenticated users can view results
    [HttpGet]
    public async Task<IActionResult> GetResults()
    {
        var results = await _context.Results
            .AsNoTracking()
            .OrderBy(r => r.OverallPosition)
            .ToListAsync();

        return Ok(results);
    }

    //GET: api/Results/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetResult(int id)
    {
        var result = await _context.Results
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ResultID == id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Result not found."
            });
        }

        return Ok(result);
    }

    //GET: api/Results/enrolment/5
    [HttpGet("enrolment/{enrolmentId:int}")]
    public async Task<IActionResult> GetResultByEnrolment(
        int enrolmentId)
    {
        var result = await _context.Results
            .AsNoTracking()
            .FirstOrDefaultAsync(r =>
                r.EnrolmentID == enrolmentId);

        if (result == null)
        {
            return NotFound(new
            {
                message = "No result exists for this enrolment."
            });
        }

        return Ok(result);
    }

    //POST: api/Results
    //Only an organiser of the associated event can record results
    [HttpPost]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> CreateResult(
        [FromBody] ResultCreateRequest request)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var enrolment = await _context.Enrolments
            .FirstOrDefaultAsync(e =>
                e.EnrolmentID == request.EnrolmentID);

        if (enrolment == null)
        {
            return NotFound(new
            {
                message = "Enrolment not found."
            });
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.CategoryID == enrolment.CategoryID);

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
                message = "Event not found."
            });
        }

        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "You can only record results for your own events."
            });
        }

        bool resultExists = await _context.Results
            .AnyAsync(r => r.EnrolmentID == request.EnrolmentID);

        if (resultExists)
        {
            return Conflict(new
            {
                message = "A result already exists for this enrolment."
            });
        }

        if (request.ResultStatus != "Finished" &&
            request.FinishTime.HasValue)
        {
            return BadRequest(new
            {
                message = "Finish time can only be provided for a Finished result."
            });
        }

        if (request.ResultStatus == "Finished" &&
            !request.FinishTime.HasValue)
        {
            return BadRequest(new
            {
                message = "A finish time is required for a Finished result."
            });
        }

        if (request.FinishTime.HasValue &&
            request.FinishTime.Value < TimeSpan.Zero)
        {
            return BadRequest(new
            {
                message = "Finish time cannot be negative."
            });
        }

        var result = new Result
        {
            EnrolmentID = request.EnrolmentID,
            FinishTime = request.FinishTime,
            OverallPosition = request.OverallPosition,
            CategoryPosition = request.CategoryPosition,
            ResultStatus = request.ResultStatus
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetResult),
            new { id = result.ResultID },
            result);
    }

    //PUT: api/Results/5
    [HttpPut("{id:int}")]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> UpdateResult(
        int id,
        [FromBody] ResultUpdateRequest request)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var result = await _context.Results
            .FirstOrDefaultAsync(r => r.ResultID == id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Result not found."
            });
        }

        var enrolment = await _context.Enrolments
            .FirstOrDefaultAsync(e =>
                e.EnrolmentID == result.EnrolmentID);

        if (enrolment == null)
        {
            return NotFound(new
            {
                message = "The associated enrolment was not found."
            });
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.CategoryID == enrolment.CategoryID);

        if (category == null)
        {
            return NotFound(new
            {
                message = "The associated category was not found."
            });
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e =>
                e.EventID == category.EventID);

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
                message = "You can only update results for your own events."
            });
        }

        if (request.ResultStatus != "Finished" &&
            request.FinishTime.HasValue)
        {
            return BadRequest(new
            {
                message = "Finish time can only be provided for a Finished result."
            });
        }

        if (request.ResultStatus == "Finished" &&
            !request.FinishTime.HasValue)
        {
            return BadRequest(new
            {
                message = "A finish time is required for a Finished result."
            });
        }

        if (request.FinishTime.HasValue &&
            request.FinishTime.Value < TimeSpan.Zero)
        {
            return BadRequest(new
            {
                message = "Finish time cannot be negative."
            });
        }

        result.FinishTime = request.FinishTime;
        result.OverallPosition = request.OverallPosition;
        result.CategoryPosition = request.CategoryPosition;
        result.ResultStatus = request.ResultStatus;

        await _context.SaveChangesAsync();

        return Ok(result);
    }

    //DELETE: api/Results/5
    [HttpDelete("{id:int}")]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> DeleteResult(int id)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var result = await _context.Results
            .FirstOrDefaultAsync(r => r.ResultID == id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Result not found."
            });
        }

        var enrolment = await _context.Enrolments
            .FirstOrDefaultAsync(e =>
                e.EnrolmentID == result.EnrolmentID);

        if (enrolment == null)
        {
            return NotFound(new
            {
                message = "The associated enrolment was not found."
            });
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c =>
                c.CategoryID == enrolment.CategoryID);

        if (category == null)
        {
            return NotFound(new
            {
                message = "The associated category was not found."
            });
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e =>
                e.EventID == category.EventID);

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
                message = "You can only delete results for your own events."
            });
        }

        _context.Results.Remove(result);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Result deleted successfully."
        });
    }
}
