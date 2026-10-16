using Microsoft.EntityFrameworkCore;
using RaceDay.Part2.Data;
using RaceDay.Part2.DTOs;
using RaceDay.Part2.Models;
using RaceDay.Part2.Services;

namespace RaceDay.Part2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _context;
    private readonly PasswordService _passwordService;

    public AuthController(
        RaceDayContext context,
        PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    //POST: api/Auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        //Public registration must not allow users to grant themselves
        //Organiser privileges
        if (!string.Equals(
                request.Role,
                "Participant",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "Public registration is only available for Participants."
            });
        }

        string email = request.Email.Trim();

        //Prevent duplicate accounts
        bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == email);

        if (emailExists)
        {
            return Conflict(new
            {
                message = "An account with this email already exists."
            });
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = "Participant",
            Phone = request.Phone?.Trim()
        };

        //Store the password hash, never the original password
        user.PasswordHash = _passwordService.HashPassword(
            user,
            request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Created(
            $"/api/Auth/{user.UserID}",
            new
            {
                user.UserID,
                user.FullName,
                user.Email,
                user.Role
            });
    }

    //POST: api/Auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        string email = request.Email.Trim();

        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.Email == email);

        if (user == null ||
            !_passwordService.VerifyPassword(user, request.Password))
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        //Clear any previous identity before starting a new session
        HttpContext.Session.Clear();

        //Store the authenticated identity on the server-side session
        HttpContext.Session.SetInt32("UserID", user.UserID);
        HttpContext.Session.SetString("Role", user.Role);

        return Ok(new
        {
            message = "Login successful.",
            user.UserID,
            user.FullName,
            user.Email,
            user.Role
        });
    }

    //POST: api/Auth/logout
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return Ok(new
        {
            message = "Logout successful."
        });
    }

    //GET: api/Auth/session
    [HttpGet("session")]
    public async Task<IActionResult> GetSession()
    {
        int? userId = HttpContext.Session.GetInt32("UserID");
        string? role = HttpContext.Session.GetString("Role");

        if (userId == null || role == null)
        {
            return Unauthorized(new
            {
                message = "You are not logged in."
            });
        }

        var user = await _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.UserID == userId.Value);

        if (user == null || user.Role != role)
        {
            HttpContext.Session.Clear();

            return Unauthorized(new
            {
                message = "Your session is invalid. Please log in again."
            });
        }

        return Ok(new
        {
            user.UserID,
            user.FullName,
            user.Email,
            user.Role
        });
    }
}

