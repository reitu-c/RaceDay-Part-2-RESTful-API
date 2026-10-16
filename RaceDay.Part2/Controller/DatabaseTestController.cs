using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Part2.Data;

namespace RaceDay.Part2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseTestController : ControllerBase
{
    private readonly RaceDayContext _context;

    public DatabaseTestController(RaceDayContext context)
    {
        _context = context;
    }

    // GET: api/DatabaseTest
    // Checks the database connection without changing any data.
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        bool connected = await _context.Database.CanConnectAsync();

        if (!connected)
        {
            return StatusCode(
                503,
                new { message = "The RaceDay database is unavailable." });
        }

        return Ok(new
        {
            message = "Successfully connected to the RaceDay database."
        });
    }
}

