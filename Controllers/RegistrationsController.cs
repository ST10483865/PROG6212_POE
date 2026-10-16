using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_api.Data;
using RaceDay_api.Dtos;
using RaceDay_api.Helpers;
using RaceDay_api.Models;

namespace RaceDay_api.Controllers;

[ApiController]
public class RegistrationsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public RegistrationsController(RaceDayDbContext db) => _db = db;

    private IQueryable<RegistrationResponse> Project(IQueryable<Registration> q) =>
        q.Select(r => new RegistrationResponse(
            r.RegistrationId, r.CategoryId, r.Category.CategoryName,
            r.Category.EventId, r.Category.Event.Name,
            r.ParticipantId, r.Participant.User.Name,
            r.RaceNum, r.RegistrationDate));

    /// <summary>Enters the logged-in Participant into an event by selecting a category.</summary>
    /// <remarks>
    /// Records the link between the Participant, the event and the chosen category, and generates a unique race number.
    /// A participant can enter each event once, and a category cannot exceed its maximum number of participants.
    /// </remarks>
    /// <response code="201">Enrolment recorded. Returns the RegistrationId and RaceNum.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser, or ParticipantId is not the caller's own.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Category is full, or the participant has already entered this event.</response>
    [HttpPost("/api/registrations")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] RegistrationRequest req)
    {
        var participantId = User.GetProfileId();
        if (req.ParticipantId.HasValue && req.ParticipantId.Value != participantId) return Forbid();

        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == req.CategoryId);
        if (category is null) return NotFound(new MessageResponse("Category not found."));

        if (await _db.Registrations.AnyAsync(r => r.ParticipantId == participantId && r.Category.EventId == category.EventId))
            return Conflict(new MessageResponse("You have already entered this event."));

        var inCategory = await _db.Registrations.CountAsync(r => r.CategoryId == category.CategoryId);
        if (category.MaxParticipants.HasValue && inCategory >= category.MaxParticipants.Value)
            return Conflict(new MessageResponse("This category is full."));

        // Race number: RD<event>-<sequence>, bumped until unique.
        var seq = await _db.Registrations.CountAsync(r => r.Category.EventId == category.EventId) + 1;
        string raceNum;
        do { raceNum = $"RD{category.EventId:D3}-{seq++:D4}"; }
        while (await _db.Registrations.AnyAsync(r => r.RaceNum == raceNum));

        var registration = new Registration
        {
            CategoryId = category.CategoryId,
            ParticipantId = participantId,
            RaceNum = raceNum,
            RegistrationDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        _db.Registrations.Add(registration);
        await _db.SaveChangesAsync();

        var dto = await Project(_db.Registrations.AsNoTracking().Where(r => r.RegistrationId == registration.RegistrationId))
            .FirstAsync();
        return Created($"/api/registrations/{registration.RegistrationId}", dto);
    }

    /// <summary>Lists every event the logged-in Participant has entered (past and upcoming).</summary>
    /// <response code="200">Array of the participant's registrations.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Organiser, or the ID belongs to a different participant.</response>
    [HttpGet("/api/participants/{id:int}/registrations")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(IEnumerable<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetForParticipant(int id)
    {
        if (id != User.GetProfileId()) return Forbid();
        var list = await Project(_db.Registrations.AsNoTracking().Where(r => r.ParticipantId == id)
            .OrderBy(r => r.RegistrationId)).ToListAsync();
        return Ok(list);
    }

    /// <summary>Lists all enrolments for one of the Organiser's events.</summary>
    /// <response code="200">Array of registrations, including participant name and race number.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("/api/events/{eventId:int}/registrations")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        var list = await Project(_db.Registrations.AsNoTracking().Where(r => r.Category.EventId == eventId)
            .OrderBy(r => r.RegistrationId)).ToListAsync();
        return Ok(list);
    }

    /// <summary>Gets one registration. Visible to the participant who owns it and to the event's Organiser.</summary>
    /// <response code="200">The registration.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not the owning participant or the event's organiser.</response>
    /// <response code="404">Registration not found.</response>
    [HttpGet("/api/registrations/{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var reg = await _db.Registrations.AsNoTracking().Include(r => r.Category).ThenInclude(c => c.Event)
            .FirstOrDefaultAsync(r => r.RegistrationId == id);
        if (reg is null) return NotFound();

        var profileId = User.GetProfileId();
        var allowed = User.IsInRole(AppRoles.Participant)
            ? reg.ParticipantId == profileId
            : reg.Category.Event.OrganiserId == profileId;
        if (!allowed) return Forbid();

        var dto = await Project(_db.Registrations.AsNoTracking().Where(r => r.RegistrationId == id)).FirstAsync();
        return Ok(dto);
    }

    /// <summary>Withdraws the logged-in Participant from an event.</summary>
    /// <response code="200">Registration cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Organiser, or the registration belongs to another participant.</response>
    /// <response code="404">Registration not found.</response>
    [HttpDelete("/api/registrations/{id:int}")]
    [Authorize(Roles = AppRoles.Participant)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var reg = await _db.Registrations.Include(r => r.Result).FirstOrDefaultAsync(r => r.RegistrationId == id);
        if (reg is null) return NotFound();
        if (reg.ParticipantId != User.GetProfileId()) return Forbid();

        _db.Registrations.Remove(reg);
        await _db.SaveChangesAsync();
        return Ok(new MessageResponse("Registration cancelled."));
    }

}
