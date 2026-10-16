using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_api.Data;
using RaceDay_api.Dtos;
using RaceDay_api.Helpers;
using RaceDay_api.Models;

namespace RaceDay_api.Controllers;


[ApiController]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public EventsController(RaceDayDbContext db) => _db = db;

    internal static EventResponse ToResponse(Event e)
    {
        var route = e.Routes.OrderBy(r => r.RouteId).FirstOrDefault();
        return new EventResponse(e.EventId, e.OrganiserId, e.Name, e.Description, e.EventType, e.Location, e.Date, e.Time,
            route is null ? null : new RouteResponse(route.RouteId, route.StartingPoint, route.EndingPoint, route.Distance));
    }

    /// <summary>Lists events, optionally filtered.</summary>
    /// <param name="location">Optional. Events whose location contains this text.</param>
    /// <param name="date">Optional. Events on this date (yyyy-MM-dd).</param>
    /// <param name="type">Optional. Run, Walk or Cycle.</param>
    /// <response code="200">Array of events ordered by date, each with its route (including distance).</response>
    /// <response code="400">The type filter is not Run, Walk or Cycle.</response>
    [HttpGet("/api/events")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] string? location, [FromQuery] DateOnly? date, [FromQuery] string? type)
    {
        var query = _db.Events.AsNoTracking().Include(e => e.Routes).AsQueryable();

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(e => e.Location.Contains(location));
        if (date.HasValue)
            query = query.Where(e => e.Date == date.Value);
        if (!string.IsNullOrWhiteSpace(type))
        {
            var normalised = EventTypes.Normalise(type);
            if (normalised is null) return BadRequest(new MessageResponse("type must be Run, Walk or Cycle."));
            query = query.Where(e => e.EventType == normalised);
        }

        var events = await query.OrderBy(e => e.Date).ThenBy(e => e.Time).ToListAsync();
        return Ok(events.Select(ToResponse));
    }

    /// <summary>Gets one event, including its route.</summary>
    /// <response code="200">The event with its nested route.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("/api/events/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking().Include(e => e.Routes).FirstOrDefaultAsync(e => e.EventId == id);
        return ev is null ? NotFound() : Ok(ToResponse(ev));
    }

    /// <summary>Creates a new event (with its route). Organisers only.</summary>
    /// <remarks>The event is automatically owned by the logged-in Organiser.</remarks>
    /// <response code="201">Event created. Returns the event including its new EventId.</response>
    /// <response code="400">Validation failed (e.g. EventType is not Run, Walk or Cycle).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost("/api/events")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] EventRequest req)
    {
        var type = EventTypes.Normalise(req.EventType);
        if (type is null) return BadRequest(new MessageResponse("EventType must be Run, Walk or Cycle."));

        var ev = new Event
        {
            OrganiserId = User.GetProfileId(),
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            EventType = type,
            Location = req.Location.Trim(),
            Date = req.Date,
            Time = req.Time,
            Routes =
            {
                new RaceRoute
                {
                    StartingPoint = req.StartingPoint.Trim(),
                    EndingPoint = req.EndingPoint.Trim(),
                    Distance = req.Distance
                }
            }
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        return Created($"/api/events/{ev.EventId}", ToResponse(ev));
    }

    /// <summary>Updates an event. Only the Organiser who owns it.</summary>
    /// <response code="200">Returns the updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("/api/events/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] EventUpdateRequest req)
    {
        var type = EventTypes.Normalise(req.EventType);
        if (type is null) return BadRequest(new MessageResponse("EventType must be Run, Walk or Cycle."));

        var ev = await _db.Events.Include(e => e.Routes).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        ev.Name = req.Name.Trim();
        ev.Description = req.Description?.Trim();
        ev.EventType = type;
        ev.Location = req.Location.Trim();
        ev.Date = req.Date;
        ev.Time = req.Time;

        var route = ev.Routes.OrderBy(r => r.RouteId).FirstOrDefault();
        if (route is not null)
        {
            if (!string.IsNullOrWhiteSpace(req.StartingPoint)) route.StartingPoint = req.StartingPoint.Trim();
            if (!string.IsNullOrWhiteSpace(req.EndingPoint)) route.EndingPoint = req.EndingPoint.Trim();
            if (req.Distance.HasValue) route.Distance = req.Distance.Value;
        }

        await _db.SaveChangesAsync();
        return Ok(ToResponse(ev));
    }

    /// <summary>Deletes an event with its route, categories, registrations and results. Only the owning Organiser.</summary>
    /// <response code="200">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("/api/events/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return Ok(new MessageResponse("Event deleted."));
    }

}
