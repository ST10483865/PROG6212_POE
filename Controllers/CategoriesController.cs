using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_api.Data;
using RaceDay_api.Dtos;
using RaceDay_api.Helpers;
using RaceDay_api.Models;

namespace RaceDay_api.Controllers;


[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    public CategoriesController(RaceDayDbContext db) => _db = db;

    private static CategoryResponse ToResponse(Category c, int registered) =>
        new(c.CategoryId, c.EventId, c.CategoryName, c.EntryFee, c.MaxParticipants, registered);

    /// <summary>Lists all categories for an event (e.g. 10km, 21km, Under 20, Senior).</summary>
    /// <response code="200">Array of categories with how many participants have entered each.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("/api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound();

        var list = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryId)
            .Select(c => new CategoryResponse(c.CategoryId, c.EventId, c.CategoryName, c.EntryFee,
                c.MaxParticipants, c.Registrations.Count))
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>Adds a category to an event. Only the owning Organiser.</summary>
    /// <response code="201">Category created. Returns the new CategoryId.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("/api/events/{eventId:int}/categories")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CategoryRequest req)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        var category = new Category
        {
            EventId = eventId,
            CategoryName = req.CategoryName.Trim(),
            EntryFee = req.EntryFee,
            MaxParticipants = req.MaxParticipants
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return Created($"/api/events/{eventId}/categories", ToResponse(category, 0));
    }

    /// <summary>Updates a category. Only the Organiser who owns its event.</summary>
    /// <response code="200">Returns the updated category.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("/api/categories/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest req)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != User.GetProfileId()) return Forbid();

        category.CategoryName = req.CategoryName.Trim();
        category.EntryFee = req.EntryFee;
        category.MaxParticipants = req.MaxParticipants;
        await _db.SaveChangesAsync();

        var registered = await _db.Registrations.CountAsync(r => r.CategoryId == id);
        return Ok(ToResponse(category, registered));
    }

    /// <summary>Removes a category. Only the owning Organiser, and only if nobody has entered it yet.</summary>
    /// <response code="200">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Participant, or an Organiser who does not own this event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Participants are already registered in this category.</response>
    [HttpDelete("/api/categories/{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != User.GetProfileId()) return Forbid();
        if (await _db.Registrations.AnyAsync(r => r.CategoryId == id))
            return Conflict(new MessageResponse("Participants are already registered in this category."));

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return Ok(new MessageResponse("Category deleted."));
    }

}
