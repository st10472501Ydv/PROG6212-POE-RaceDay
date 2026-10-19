using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Models;
using RaceDay.API.Security;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _db;

    public AuthController(RaceDayContext db) => _db = db;

    /// <summary>Creates a new account. The user picks Organiser or Participant during signup.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "fullName, email and password are required." });

        if (request.Role != "Organiser" && request.Role != "Participant")
            return BadRequest(new { message = "role must be Organiser or Participant." });

        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest(new { message = "An account with this email already exists." });

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = request.Role,
            DateJoined = DateTime.Now
        };
        _db.Users.Add(user);
        if (user.Role == "Organiser")
            _db.OrganiserDetails.Add(new OrganiserDetail { User = user });   // organisers get their club row
        await _db.SaveChangesAsync();

        return Created($"/api/users/profile", new { userId = user.UserID, message = "Account created." });
    }

    /// <summary>Signs the user in. The server stores their ID and role in the session and sends back a session cookie.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        string email = request.Email ?? "";
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !PasswordHasher.Verify(request.Password ?? "", user.PasswordHash))
            return Unauthorized(new { message = "Email or password is wrong." });

        HttpContext.Session.SetInt32("UserId", user.UserID);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("FullName", user.FullName);

        return Ok(new { userId = user.UserID, fullName = user.FullName, role = user.Role });
    }

    /// <summary>Signs the user out by clearing their session on the server.</summary>
    [HttpPost("logout")]
    [SessionAuth]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}

public record RegisterRequest(string? FullName, string? Email, string? Password, string? Role);
public record LoginRequest(string? Email, string? Password);
