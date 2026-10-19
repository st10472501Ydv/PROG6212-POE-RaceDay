using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Auth;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private static readonly string[] Statuses = { "Finished", "DNF", "Disqualified" };
    private readonly RaceDayContext _db;

    public ResultsController(RaceDayContext db) => _db = db;

    private int UserId => HttpContext.Session.GetInt32("UserId")!.Value;
    private ObjectResult Forbidden(string message) => StatusCode(403, new { message });

    /// <summary>Shows the results for an event, sorted by finishing position.</summary>
    [HttpGet("events/{eventId:int}/results")]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventID == eventId))
            return NotFound(new { message = "Event does not exist." });

        var rows = await _db.Results
            .Where(r => r.Enrolment!.Category!.EventID == eventId)
            .OrderBy(r => r.Position)
            .Select(r => new
            {
                resultId = r.ResultID,
                participantName = r.Enrolment!.Participant!.FullName,
                categoryName = r.Enrolment!.Category!.CategoryName,
                finishTime = r.FinishTime,
                position = r.Position,
                status = r.Status
            })
            .ToListAsync();
        return Ok(rows);
    }

    /// <summary>Captures a finish time and position for a participant after an event. Only the event's organiser can do this.</summary>
    [HttpPost("events/{eventId:int}/results")]
    [SessionAuth("Organiser")]
    public async Task<IActionResult> CaptureResult(int eventId, ResultRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null)
            return NotFound(new { message = "Event does not exist." });
        if (ev.OrganiserID != UserId)
            return Forbidden("Only the organiser who created this event can capture results.");

        string? error = ValidateResult(request);
        if (error != null)
            return BadRequest(new { message = error });

        var enrolment = await _db.Enrolments
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.EnrolmentID == request.EnrolmentId);
        if (enrolment == null)
            return BadRequest(new { message = "enrolmentId is not a valid enrolment." });
        if (enrolment.Category == null || enrolment.Category.EventID != eventId)
            return BadRequest(new { message = "That enrolment is not part of this event." });
        if (await _db.Results.AnyAsync(r => r.EnrolmentID == enrolment.EnrolmentID))
            return Conflict(new { message = "A result already exists for this enrolment." });

        var result = new Result
        {
            EnrolmentID = enrolment.EnrolmentID,
            FinishTime = request.FinishTime!.Value,
            Position = request.Position,
            Status = request.Status ?? "Finished"
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return Created($"/api/results/{result.ResultID}",
            new { resultId = result.ResultID, message = "Result captured." });
    }

    /// <summary>Updates a result. Only the event's organiser can do this.</summary>
    [HttpPut("results/{id:int}")]
    [SessionAuth("Organiser")]
    public async Task<IActionResult> UpdateResult(int id, ResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .FirstOrDefaultAsync(r => r.ResultID == id);
        if (result == null)
            return NotFound(new { message = "Result does not exist." });
        if (result.Enrolment?.Category?.Event?.OrganiserID != UserId)
            return Forbidden("Only the organiser who created the event can change results.");

        string? error = ValidateResult(request, requireEnrolment: false);
        if (error != null)
            return BadRequest(new { message = error });

        result.FinishTime = request.FinishTime!.Value;
        result.Position = request.Position;
        result.Status = request.Status ?? "Finished";
        await _db.SaveChangesAsync();

        return Ok(new
        {
            resultId = result.ResultID,
            enrolmentId = result.EnrolmentID,
            finishTime = result.FinishTime,
            position = result.Position,
            status = result.Status,
            message = "Result updated."
        });
    }

    /// <summary>Shows the logged-in user's own results across every event they entered.</summary>
    [HttpGet("users/results")]
    [SessionAuth]
    public async Task<IActionResult> GetMyResults()
    {
        int userId = UserId;
        var rows = await _db.Results
            .Where(r => r.Enrolment!.ParticipantID == userId)
            .OrderBy(r => r.Position)
            .Select(r => new
            {
                resultId = r.ResultID,
                eventName = r.Enrolment!.Category!.Event!.EventName,
                eventDate = r.Enrolment!.Category!.Event!.EventDate,
                categoryName = r.Enrolment!.Category!.CategoryName,
                finishTime = r.FinishTime,
                position = r.Position,
                status = r.Status
            })
            .ToListAsync();
        return Ok(rows);
    }

    private static string? ValidateResult(ResultRequest r, bool requireEnrolment = true)
    {
        if (r.FinishTime is null)
            return "finishTime is required (HH:MM:SS).";
        if (requireEnrolment && r.EnrolmentId is null)
            return "enrolmentId is required.";
        if (r.Status != null && !Statuses.Contains(r.Status))
            return "status must be Finished, DNF or Disqualified.";
        return null;
    }
}

public record ResultRequest(int? EnrolmentId, TimeSpan? FinishTime, int? Position, string? Status);
