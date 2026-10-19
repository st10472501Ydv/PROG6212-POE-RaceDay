using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public EnrolmentsController(RaceDayContext db) => _db = db;

    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;
    private ObjectResult Forbidden(string message) => StatusCode(403, new { message });

    /// <summary>Lets a participant enter a category. Checks for double entries and full categories.</summary>
    [HttpPost("categories/{categoryId:int}/enrol")]
    [SessionAuth("Participant")]
    public async Task<IActionResult> Enrol(int categoryId)
    {
        var category = await _db.Categories.FindAsync(categoryId);
        if (category == null)
            return NotFound(new { message = "Category does not exist." });

        if (await _db.Enrolments.AnyAsync(e => e.CategoryID == categoryId && e.ParticipantID == UserId))
            return Conflict(new { message = "You are already enrolled in this category." });

        if (category.MaxParticipants is > 0 &&
            await _db.Enrolments.CountAsync(e => e.CategoryID == categoryId) >= category.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        var enrolment = new Enrolment
        {
            ParticipantID = UserId,
            CategoryID = categoryId,
            EnrolmentDate = DateTime.Now
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return Created($"/api/users/enrolments",
            new { enrolmentId = enrolment.EnrolmentID, message = "You are enrolled." });
    }

    /// <summary>Shows all the events the logged-in participant has signed up for.</summary>
    [HttpGet("users/enrolments")]
    [SessionAuth("Participant")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        int userId = UserId;
        var rows = await _db.Enrolments
            .Where(e => e.ParticipantID == userId)
            .OrderByDescending(e => e.EnrolmentDate)
            .Select(e => new
            {
                enrolmentId = e.EnrolmentID,
                enrolmentDate = e.EnrolmentDate,
                eventId = e.Category!.EventID,
                eventName = e.Category!.Event!.EventName,
                eventDate = e.Category!.Event!.EventDate,
                categoryId = e.CategoryID,
                categoryName = e.Category!.CategoryName
            })
            .ToListAsync();
        return Ok(rows);
    }

    /// <summary>Cancels an enrolment. A participant can only cancel their own.</summary>
    [HttpDelete("enrolments/{id:int}")]
    [SessionAuth("Participant")]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _db.Enrolments.FindAsync(id);
        if (enrolment == null)
            return NotFound(new { message = "Enrolment does not exist." });
        if (enrolment.ParticipantID != UserId)
            return Forbidden("You can only cancel your own enrolments.");

        _db.Enrolments.Remove(enrolment);   // any result for it cascades in the database
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>Shows every enrolment for one of the organiser's own events.</summary>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuth("Organiser")]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserID != UserId)
            return Forbidden("Only the organiser who created this event can see its enrolments.");

        var rows = await _db.Enrolments
            .Where(e => e.Category!.EventID == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .Select(e => new
            {
                enrolmentId = e.EnrolmentID,
                enrolmentDate = e.EnrolmentDate,
                participantId = e.ParticipantID,
                participantName = e.Participant!.FullName,
                categoryId = e.CategoryID,
                categoryName = e.Category!.CategoryName
            })
            .ToListAsync();
        return Ok(rows);
    }
}
