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
public class CategoriesController : ControllerBase
{
    private readonly RaceDayContext _context;

    public CategoriesController(RaceDayContext context)
    {
        _context = context;
    }

    //GET: api/Categories
    //All logged-in users can view categories
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.EventID)
            .ThenBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(categories);
    }

    // GET: api/Categories/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategory(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        return Ok(category);
    }

    //GET: api/Categories/event/5
    //View categories belonging to a particular event
    [HttpGet("event/{eventId:int}")]
    public async Task<IActionResult> GetCategoriesByEvent(int eventId)
    {
        var eventExists = await _context.Events
            .AnyAsync(e => e.EventID == eventId);

        if (!eventExists)
        {
            return NotFound(new
            {
                message = "Event not found."
            });
        }

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        return Ok(categories);
    }

    //POST: api/Categories
    //Only an organiser who owns the event can add a category
    [HttpPost]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CategoryCreateRequest request)
    {
        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

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
                message = "You can only add categories to your own events."
            });
        }

        if (raceEvent.Status != "Open")
        {
            return Conflict(new
            {
                message = "Categories can only be added to open events."
            });
        }

        bool categoryExists = await _context.Categories.AnyAsync(c =>
            c.EventID == request.EventID &&
            c.CategoryName == request.CategoryName.Trim());

        if (categoryExists)
        {
            return Conflict(new
            {
                message = "This category name already exists for the event."
            });
        }

        var category = new Category
        {
            EventID = request.EventID,
            CategoryName = request.CategoryName.Trim(),
            DistanceKM = request.DistanceKM,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.CategoryID },
            category);
    }

    //PUT: api/Categories/5
    //Only the organiser who owns the event can update its category
    [HttpPut("{id:int}")]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> UpdateCategory(
        int id,
        [FromBody] CategoryUpdateRequest request)
    {
        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == category.EventID);

        if (raceEvent == null)
        {
            return NotFound(new
            {
                message = "The event associated with this category was not found."
            });
        }

        if (raceEvent.OrganiserID != organiserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "You can only update categories for your own events."
            });
        }

        if (raceEvent.Status != "Open")
        {
            return Conflict(new
            {
                message = "Categories can only be updated for open events."
            });
        }

        string categoryName = request.CategoryName.Trim();

        bool duplicateExists = await _context.Categories.AnyAsync(c =>
            c.EventID == category.EventID &&
            c.CategoryID != id &&
            c.CategoryName == categoryName);

        if (duplicateExists)
        {
            return Conflict(new
            {
                message = "Another category with this name already exists for the event."
            });
        }

        category.CategoryName = categoryName;
        category.DistanceKM = request.DistanceKM;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;

        await _context.SaveChangesAsync();

        return Ok(category);
    }

    //DELETE: api/Categories/5
    [HttpDelete("{id:int}")]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        int organiserId = HttpContext.Session.GetInt32("UserID")!.Value;

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.CategoryID == id);

        if (category == null)
        {
            return NotFound(new
            {
                message = "Category not found."
            });
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == category.EventID);

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
                message = "You can only delete categories for your own events."
            });
        }

        bool hasEnrolments = await _context.Enrolments
            .AnyAsync(e => e.CategoryID == id);

        if (hasEnrolments)
        {
            return Conflict(new
            {
                message = "This category cannot be deleted because enrolments are linked to it."
            });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Category deleted successfully."
        });
    }
}

