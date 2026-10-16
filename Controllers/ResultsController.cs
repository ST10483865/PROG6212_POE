using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_api.Data;
using RaceDay_api.Dtos;
using RaceDay_api.Helpers;
using RaceDay_api.Models;

namespace RaceDay_api.Controllers;


[ApiController]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public ResultsController(RaceDayDbContext db) => _db = db;

    private static IQueryable<ResultResponse> Project(IQueryable<Result> q) =>
        q.Select(x => new ResultResponse(
            x.ResultId, x.RegistrationId,
            x.Registration.Category.EventId, x.Registration.Category.Event.Name, x.Registration.Category.CategoryName,
            x.Registration.ParticipantId, x.Registration.Participant.User.Name,
            x.Registration.RaceNum, x.FinishTime, x.Position));

    /// <summary>Captures a finish time and position for a participant's registration. Organisers only.</summary>
    /// <response code="201">Result recorded. Returns the new ResultId.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">Registration not found.</response>
    /// <response code="409">A result already exists for this registration.</response>
    [HttpPost("/api/results")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ResultRequest req)
    {
        var reg = await _db.Registrations.AsNoTracking().Include(r => r.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.RegistrationId == req.RegistrationId);
        if (reg is null) return NotFound(new MessageResponse("Registration not found."));
        if (reg.Category.Event.OrganiserId != User.GetProfileId()) return Forbid();
        if (await _db.Results.AnyAsync(r => r.RegistrationId == req.RegistrationId))
            return Conflict(new MessageResponse("A result already exists for this registration."));

        var result = new Result { RegistrationId = req.RegistrationId, FinishTime = req.FinishTime, Position = req.Position };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var dto = await Project(_db.Results.AsNoTracking().Where(x => x.ResultId == result.ResultId)).FirstAsync();
        return Created($"/api/results/{result.ResultId}", dto);
    }

    /// <summary>Public leaderboard for an event, ordered by finishing position.</summary>
    /// <response code="200">Array of results ordered by position.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("/api/events/{eventId:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        var list = await Project(_db.Results.AsNoTracking()
            .Where(x => x.Registration.Category.EventId == eventId)
            .OrderBy(x => x.Position == null).ThenBy(x => x.Position).ThenBy(x => x.FinishTime)).ToListAsync();
        return Ok(list);
    }

    /// <summary>Gets the logged-in Participant's own result history.</summary>
    /// <response code="200">Array of the participant's results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Organiser, or the ID belongs to a different participant.</response>
    [HttpGet("/api/participants/{id:int}/results")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForParticipant(int id)
    {
        if (id != User.GetProfileId()) return Forbid();
        var list = await Project(_db.Results.AsNoTracking().Where(x => x.Registration.ParticipantId == id)
            .OrderBy(x => x.ResultId)).ToListAsync();
        return Ok(list);
    }

    /// <summary>Corrects a captured result. Only the Organiser who owns the event.</summary>
    /// <response code="200">Returns the updated result.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own the event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("/api/results/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ResultUpdateRequest req)
    {
        var result = await _db.Results.Include(x => x.Registration).ThenInclude(r => r.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(x => x.ResultId == id);
        if (result is null) return NotFound();
        if (result.Registration.Category.Event.OrganiserId != User.GetProfileId()) return Forbid();

        result.FinishTime = req.FinishTime;
        result.Position = req.Position;
        await _db.SaveChangesAsync();

        var dto = await Project(_db.Results.AsNoTracking().Where(x => x.ResultId == id)).FirstAsync();
        return Ok(dto);
    }

}
