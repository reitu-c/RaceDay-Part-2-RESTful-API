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
public class RoutesController : ControllerBase
{
    private readonly RaceDayContext _context;

    public RoutesController(RaceDayContext context)
    {
        _context = context;
    }

    //GET: api/Routes
    //Authenticated users can view all routes
    [HttpGet]
    public async Task<IActionResult> GetRoutes()
    {
        var routes = await _context.Routes
            .AsNoTracking()
            .OrderBy(r => r.EventID)
            .ToListAsync();

        return Ok(routes);
    }

    // GET: api/Routes/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRoute(int id)
    {
        var route = await _context.Routes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RouteID == id);

        if (route == null)
        {
            return NotFound(new
            {
                message = "Route not found."
            });
        }

        return Ok(route);
    }

    //GET: api/Routes/event/5
    [HttpGet("event/{eventId:int}")]
    public async Task<IActionResult> GetRoutesByEvent(int eventId)
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

        var routes = await _context.Routes
            .AsNoTracking()
            .Where(r => r.EventID == eventId)
            .ToListAsync();

        return Ok(routes);
    }

    //POST: api/Routes
    //Only the organiser who owns the event can create its route
    [HttpPost]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> CreateRoute(
        [FromBody] RouteCreateRequest request)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

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
                message = "You can only manage routes for your own events."
            });
        }

        if (raceEvent.Status != "Open")
        {
            return Conflict(new
            {
                message = "Routes can only be created for open events."
            });
        }

        bool routeExists = await _context.Routes
            .AnyAsync(r => r.EventID == request.EventID);

        if (routeExists)
        {
            return Conflict(new
            {
                message = "A route already exists for this event."
            });
        }

        var route = new RaceRoute
        {
            EventID = request.EventID,
            RouteName = request.RouteName.Trim(),
            DistanceKM = request.DistanceKM,
            StartPoint = request.StartPoint.Trim(),
            FinishPoint = request.FinishPoint.Trim(),
            RouteURL = request.RouteURL?.Trim()
        };

        _context.Routes.Add(route);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetRoute),
            new { id = route.RouteID },
            route);
    }

    //PUT: api/Routes/5
    [HttpPut("{id:int}")]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> UpdateRoute(
        int id,
        [FromBody] RouteUpdateRequest request)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var route = await _context.Routes
            .FirstOrDefaultAsync(r => r.RouteID == id);

        if (route == null)
        {
            return NotFound(new
            {
                message = "Route not found."
            });
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == route.EventID);

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
                message = "You can only manage routes for your own events."
            });
        }

        if (raceEvent.Status != "Open")
        {
            return Conflict(new
            {
                message = "Routes can only be updated for open events."
            });
        }

        route.RouteName = request.RouteName.Trim();
        route.DistanceKM = request.DistanceKM;
        route.StartPoint = request.StartPoint.Trim();
        route.FinishPoint = request.FinishPoint.Trim();
        route.RouteURL = request.RouteURL?.Trim();

        await _context.SaveChangesAsync();

        return Ok(route);
    }

    // DELETE: api/Routes/5
    [HttpDelete("{id:int}")]
    [SessionAuthorization("Organiser")]
    public async Task<IActionResult> DeleteRoute(int id)
    {
        int organiserId =
            HttpContext.Session.GetInt32("UserID")!.Value;

        var route = await _context.Routes
            .FirstOrDefaultAsync(r => r.RouteID == id);

        if (route == null)
        {
            return NotFound(new
            {
                message = "Route not found."
            });
        }

        var raceEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.EventID == route.EventID);

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
                message = "You can only manage routes for your own events."
            });
        }

        _context.Routes.Remove(route);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Route deleted successfully."
        });
    }
}
