using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly RaceDayContext _db;

    public UsersController(RaceDayContext db) => _db = db;

    /// <summary>Gets the profile (name, email, role, date joined) of the logged-in user.</summary>
    [HttpGet("profile")]
    [SessionAuth]
    public async Task<IActionResult> GetProfile()
    {
        int id = HttpContext.Session.GetInt32("UserId")!.Value;
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return Unauthorized(new { message = "The account in this session no longer exists." });

        return Ok(new { userId = user.UserID, fullName = user.FullName, email = user.Email, role = user.Role, dateJoined = user.DateJoined });
    }

    /// <summary>Updates the name and email of the logged-in user.</summary>
    [HttpPut("profile")]
    [SessionAuth]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "fullName and email are required." });

        int id = HttpContext.Session.GetInt32("UserId")!.Value;
        if (await _db.Users.AnyAsync(u => u.Email == request.Email && u.UserID != id))
            return BadRequest(new { message = "Another account already uses this email." });

        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return Unauthorized(new { message = "The account in this session no longer exists." });

        user.FullName = request.FullName;
        user.Email = request.Email;
        await _db.SaveChangesAsync();

        return Ok(new { userId = user.UserID, fullName = user.FullName, email = user.Email, role = user.Role, dateJoined = user.DateJoined });
    }
}

public record UpdateProfileRequest(string? FullName, string? Email);
