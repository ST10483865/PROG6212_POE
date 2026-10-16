using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_api.Data;
using RaceDay_api.Dtos;
using RaceDay_api.Helpers;
using RaceDay_api.Models;
using System.Security.Claims;
using AppUser = RaceDay_api.Models.User;

namespace RaceDay_api.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;

    public AuthController(RaceDayDbContext db, IPasswordHasher<AppUser> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    /// <summary>Registers a new account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// Creates the User and the matching Organiser/Participant profile together in one save.
    /// Only "Organiser" and "Participant" are accepted as roles, so nobody can self-assign anything else.
    /// The password is stored as a salted hash, never in its original form.
    /// </remarks>
    /// <response code="201">Account created. Returns the new UserId and the role-specific profile ID.</response>
    /// <response code="400">Validation failed (missing fields, invalid email, short password or invalid role).</response>
    /// <response code="409">An account with that email already exists.</response>
    [HttpPost("/api/auth/register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var role = AppRoles.Normalise(req.Role);
        if (role is null)
            return BadRequest(new MessageResponse("Role must be either 'Organiser' or 'Participant'."));

        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new MessageResponse("An account with this email already exists."));

        var user = new AppUser
        {
            Name = req.Name.Trim(),
            Email = email,
            Gender = req.Gender.Trim(),
            Contact = req.Contact?.Trim()
        };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        if (role == AppRoles.Organiser)
            user.Organiser = new Organiser
            {
                OrganiserName = user.Name,
                ClubName = string.IsNullOrWhiteSpace(req.ClubName) ? "Independent" : req.ClubName.Trim()
            };
        else
            user.Participant = new Participant { MedicalInfo = req.MedicalInfo?.Trim() };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(); // User + profile saved atomically

        var profileId = user.Organiser?.OrganiserId ?? user.Participant!.ParticipantId;
        return Created($"/api/users/{user.UserId}",
            new AuthResponse(user.UserId, profileId, user.Name, role, "Account created successfully."));
    }

    /// <summary>Logs in and starts a session.</summary>
    /// <remarks>
    /// On success a session cookie is issued that carries the user's ID and role. The browser (and Swagger UI)
    /// sends it automatically on every later request, so there is no token to copy around.
    /// </remarks>
    /// <response code="200">Logged in. Returns the user's ID, profile ID and role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("/api/auth/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .Include(u => u.Organiser)
            .Include(u => u.Participant)
            .FirstOrDefaultAsync(u => u.Email == email);

        // Same message for "no such user" and "wrong password" so accounts cannot be probed.
        if (user is null ||
            _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new MessageResponse("Invalid email or password."));

        string role;
        int profileId;
        if (user.Organiser is not null) { role = AppRoles.Organiser; profileId = user.Organiser.OrganiserId; }
        else if (user.Participant is not null) { role = AppRoles.Participant; profileId = user.Participant.ParticipantId; }
        else return Unauthorized(new MessageResponse("This account has no role assigned."));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, role),
            new(ClaimsExtensions.ProfileIdClaim, profileId.ToString())
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new AuthResponse(user.UserId, profileId, user.Name, role, "Logged in successfully."));
    }

    /// <summary>Logs out and ends the current session.</summary>
    /// <response code="200">Session ended.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("/api/auth/logout")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new MessageResponse("Logged out successfully."));
    }
}




