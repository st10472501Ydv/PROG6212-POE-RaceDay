using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayContext _db;

    public CategoriesController(RaceDayContext db) => _db = db;

    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;
    private ObjectResult Forbidden(string message) => StatusCode(403, new { message });

    /// <summary>Lists all categories for a specific event.</summary>
    [HttpGet("events/{eventId:int}/categories")]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new { message = "Event does not exist." });

        var rows = await _db.Categories
            .Where(c => c.EventID == eventId)
            .OrderBy(c => c.CategoryID)
            .Select(c => new
            {
                categoryId = c.CategoryID,
                categoryName = c.CategoryName,
                maxParticipants = c.MaxParticipants,
                entryFee = c.EntryFee
            })
            .ToListAsync();
        return Ok(rows);
    }

    /// <summary>Adds a category to an event. Only the event's organiser can do this.</summary>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuth("Organiser")]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserID != UserId)
            return Forbidden("Only the organiser who created this event can add categories.");

        string? error = ValidateCategory(request);
        if (error != null)
            return BadRequest(new { message = error });

        var category = new Category
        {
            EventID = eventId,
            CategoryName = request.CategoryName!.Trim(),
            MaxParticipants = request.MaxParticipants ?? 0,
            EntryFee = request.EntryFee ?? 0m
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories",
            new { categoryId = category.CategoryID, message = "Category added." });
    }

    /// <summary>Updates a category. Only the organiser who owns the parent event can do this.</summary>
    [HttpPut("categories/{id:int}")]
    [SessionAuth("Organiser")]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = "Category does not exist." });

        var ev = await _db.Events.FindAsync(category.EventID);
        if (ev == null)
            return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserID != UserId)
            return Forbidden("Only the organiser who created the parent event can change categories.");

        string? error = ValidateCategory(request);
        if (error != null)
            return BadRequest(new { message = error });

        category.CategoryName = request.CategoryName!.Trim();
        category.MaxParticipants = request.MaxParticipants ?? 0;
        category.EntryFee = request.EntryFee ?? 0m;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            categoryId = category.CategoryID,
            categoryName = category.CategoryName,
            maxParticipants = category.MaxParticipants,
            entryFee = category.EntryFee,
            message = "Category updated."
        });
    }

    /// <summary>Removes a category from an event; enrolments in that category go with it.</summary>
    [HttpDelete("categories/{id:int}")]
    [SessionAuth("Organiser")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null)
            return NotFound(new { message = "Category does not exist." });

        var ev = await _db.Events.FindAsync(category.EventID);
        if (ev == null)
            return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserID != UserId)
            return Forbidden("Only the organiser who created the parent event can delete categories.");

        _db.Categories.Remove(category);   // enrolments cascade in the database
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static string? ValidateCategory(CategoryRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.CategoryName))
            return "categoryName is required.";
        if (r.MaxParticipants is < 0)
            return "maxParticipants cannot be negative.";
        if (r.EntryFee is < 0)
            return "entryFee cannot be negative.";
        return null;
    }
}

public record CategoryRequest(string? CategoryName, int? MaxParticipants, decimal? EntryFee);
