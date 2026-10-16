using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_api.Data;
using RaceDay_api.Dtos;
using RaceDay_api.Helpers;
using AppUser = RaceDay_api.Models.User;

namespace RaceDay_api.Controllers;

[ApiController]
[Authorize]
public class UsersController : ControllerBase
{//start of class
    private readonly RaceDayDbContext _db;
    public UsersController(RaceDayDbContext db) => _db = db;

    private static UserResponse ToResponse(AppUser u) =>
    new(u.UserId, u.Name, u.Contact, u.Email, u.Gender,
        u.Organiser is not null ? AppRoles.Organiser : AppRoles.Participant);


    /// <summary>Gets the logged-in user's own profile.</summary>
    /// <response code="200">The user's profile (the password hash is never returned).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The ID belongs to a different user.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("/api/users/{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        if (id != User.GetUserId()) return Forbid();
        var user = await _db.Users.AsNoTracking()
            .Include(u => u.Organiser).Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.UserId == id);
        return user is null ? NotFound() : Ok(ToResponse(user));
    }

    /// <summary>Updates the logged-in user's own profile (name, contact, email, gender).</summary>
    /// <response code="200">Returns the updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The ID belongs to a different user.</response>
    /// <response code="404">User not found.</response>
    /// <response code="409">The new email is already used by another account.</response>
    [HttpPut("/api/users/{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatedUserRequest req)
    {
        if (id != User.GetUserId()) return Forbid();
        var user = await _db.Users.Include(u => u.Organiser).Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null) return NotFound();

        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.UserId != id))
            return Conflict(new MessageResponse("That email is already used by another account."));

        user.Name = req.Name.Trim();
        user.Contact = req.Contact?.Trim();
        user.Email = email;
        user.Gender = req.Gender.Trim();
        if (user.Organiser is not null) user.Organiser.OrganiserName = user.Name;

        await _db.SaveChangesAsync();
        return Ok(ToResponse(user));
    }

    /// <summary>Deletes the logged-in user's own account and ends the session.</summary>
    /// <remarks>A participant's registrations (and their results) are removed with the account.</remarks>
    /// <response code="200">Account deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The ID belongs to a different user.</response>
    /// <response code="404">User not found.</response>
    [HttpDelete("/api/users/{id:int}")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (id != User.GetUserId()) return Forbid();
        var user = await _db.Users.Include(u => u.Participant).FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null) return NotFound();

        if (user.Participant is not null)
        {
            // Registration -> Participant is NO ACTION in the schema, so remove them explicitly first.
            var regs = await _db.Registrations.Include(r => r.Result)
                .Where(r => r.ParticipantId == user.Participant.ParticipantId).ToListAsync();
            _db.Registrations.RemoveRange(regs);
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new MessageResponse("Account deleted."));
    }


}//end of class


