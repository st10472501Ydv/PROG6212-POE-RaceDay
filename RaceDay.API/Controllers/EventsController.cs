using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };
    private readonly RaceDayContext _db;

    public EventsController(RaceDayContext db) => _db = db;

    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;
    private ObjectResult Forbidden(string message) => StatusCode(403, new { message });

    /// <summary>Returns all active events. Anyone can see what events are coming up.</summary>
    [HttpGet]
    public async Task<IActionResult> GetEvents() =>
        Ok(await _db.Events
            .Where(e => e.IsActive)
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                eventId = e.EventID,
                eventName = e.EventName,
                description = e.Description,
                eventDate = e.EventDate,
                location = e.Location,
                distance = e.Distance,
                eventType = e.EventType
            })
            .ToListAsync());

    /// <summary>Gets the full details of one event, including all its categories.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events.Include(e => e.Categories)
            .FirstOrDefaultAsync(e => e.EventID == id);
        if (ev == null)
            return NotFound(new { message = "Event does not exist." });

        return Ok(new
        {
            eventId = ev.EventID,
            organiserId = ev.OrganiserID,
            eventName = ev.EventName,
            description = ev.Description,
            eventDate = ev.EventDate,
            location = ev.Location,
            distance = ev.Distance,
            eventType = ev.EventType,
            isActive = ev.IsActive,
            categories = ev.Categories.OrderBy(c => c.CategoryID).Select(c => new
            {
                categoryId = c.CategoryID,
                categoryName = c.CategoryName,
                maxParticipants = c.MaxParticipants,
                entryFee = c.EntryFee
            })
        });
    }

    /// <summary>Creates a new event. Only organisers can do this; the event is linked to their account.</summary>
    [HttpPost]
    [SessionAuth("Organiser")]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        string? error = ValidateEvent(request);
        if (error != null)
            return BadRequest(new { message = error });

        var ev = new Event
        {
            OrganiserID = UserId,
            EventName = request.EventName!.Trim(),
            Description = request.Description,
            EventDate = request.EventDate!.Value,
            Location = request.Location!.Trim(),
            Distance = request.Distance!.Trim(),
            EventType = request.EventType!,
            IsActive = true
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{ev.EventID}", new { eventId = ev.EventID, message = "Event created." });
    }

    /// <summary>Updates an event's details. Only the organiser who created it can do this.</summary>
    [HttpPut("{id:int}")]
    [SessionAuth("Organiser")]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserID != UserId)
            return Forbidden("Only the organiser who created this event can change it.");

        string? error = ValidateEvent(request);
        if (error != null)
            return BadRequest(new { message = error });

        ev.EventName = request.EventName!.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location!.Trim();
        ev.Distance = request.Distance!.Trim();
        ev.EventType = request.EventType!;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            eventId = ev.EventID,
            eventName = ev.EventName,
            description = ev.Description,
            eventDate = ev.EventDate,
            location = ev.Location,
            distance = ev.Distance,
            eventType = ev.EventType,
            message = "Event updated."
        });
    }

    /// <summary>Deletes an event; its categories, enrolments and results go with it. Only the creator can do this.</summary>
    [HttpDelete("{id:int}")]
    [SessionAuth("Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null)
            return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserID != UserId)
            return Forbidden("Only the organiser who created this event can delete it.");

        _db.Events.Remove(ev);   // categories, enrolments and results cascade in the database
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static string? ValidateEvent(EventRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.EventName) || string.IsNullOrWhiteSpace(r.Location) ||
            string.IsNullOrWhiteSpace(r.Distance) || r.EventDate is null)
            return "eventName, eventDate, location and distance are required.";
        if (!EventTypes.Contains(r.EventType ?? ""))
            return "eventType must be Run, Walk or Cycle.";
        return null;
    }
}

public record EventRequest(string? EventName, string? Description, DateTime? EventDate,
    string? Location, string? Distance, string? EventType);
