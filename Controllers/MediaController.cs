using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay_api.Data;
using RaceDay_api.Dtos;
using RaceDay_api.Helpers;
using RaceDay_api.Services;

namespace RaceDay_api.Controllers;

/// <summary>
/// Image uploads (event banners and profile pictures) stored in Azure Blob Storage.
/// All file handling goes through this API - the MVC website never talks to Azure.
/// </summary>
[ApiController]
public class MediaController : ControllerBase
{
    private const long MaxBytes = 2 * 1024 * 1024; // 2 MB

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public MediaController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    // ---------- helpers ----------

    private static async Task<string?> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please choose an image file.";
        if (file.Length > MaxBytes) return "The image must be 2 MB or smaller.";
        if (!AllowedTypes.ContainsKey(file.ContentType)) return "Only JPEG, PNG or WebP images are allowed.";
        if (!await HasImageSignatureAsync(file)) return "The file does not look like a valid image.";
        return null;
    }

    /// <summary>Checks the first bytes of the file, so a renamed .txt cannot pass as an image.</summary>
    private static async Task<bool> HasImageSignatureAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        var h = new byte[12];
        var read = await stream.ReadAsync(h.AsMemory(0, 12));
        if (read < 4) return false;

        return file.ContentType.ToLowerInvariant() switch
        {
            "image/jpeg" => h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF,
            "image/png" => h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47,
            "image/webp" => read >= 12 &&
                            h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' &&
                            h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P',
            _ => false
        };
    }

    // ---------- event banner ----------

    [HttpPost("/api/events/{id:int}/banner")]
    [Authorize(Roles = AppRoles.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await _db.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetProfileId()) return Forbid();

        var error = await ValidateAsync(file);
        if (error is not null) return BadRequest(new MessageResponse(error));

        var oldBlob = ev.BannerBlob;
        var blobName = $"events/{id}/{Guid.NewGuid():N}{AllowedTypes[file.ContentType]}";

        await using (var stream = file.OpenReadStream())
            await _blobs.UploadAsync(blobName, stream, file.ContentType);

        ev.BannerBlob = blobName;
        await _db.SaveChangesAsync();
        if (oldBlob is not null) await _blobs.DeleteAsync(oldBlob);

        return Ok(new ImageUploadResponse($"/api/events/{id}/banner", "Banner uploaded."));
    }

    /// <summary>Serves an event's banner image (read from Azure Blob Storage).</summary>
    /// <response code="200">The image file.</response>
    /// <response code="404">The event has no banner.</response>
    [HttpGet("/api/events/{id:int}/banner")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBanner(int id)
    {
        var blobName = await _db.Events.Where(e => e.EventId == id).Select(e => e.BannerBlob).FirstOrDefaultAsync();
        if (blobName is null) return NotFound();

        var blob = await _blobs.DownloadAsync(blobName);
        if (blob is null) return NotFound();
        return File(blob.Value.Content, blob.Value.ContentType);
    }

    // ---------- profile picture ----------

    /// <summary>Uploads (or replaces) the logged-in user's profile picture.</summary>
    /// <remarks>JPEG, PNG or WebP, maximum 2 MB. The image is stored in Azure Blob Storage.</remarks>
    /// <param name="file">The image file (form field name: file).</param>
    /// <response code="200">Uploaded. Returns the API path that serves the picture.</response>
    /// <response code="400">No file, too large, or not a JPEG/PNG/WebP image.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("/api/users/me/picture")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ImageUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null) return Unauthorized();

        var error = await ValidateAsync(file);
        if (error is not null) return BadRequest(new MessageResponse(error));

        var oldBlob = user.ProfilePictureBlob;
        var blobName = $"users/{userId}/{Guid.NewGuid():N}{AllowedTypes[file.ContentType]}";

        await using (var stream = file.OpenReadStream())
            await _blobs.UploadAsync(blobName, stream, file.ContentType);

        user.ProfilePictureBlob = blobName;
        await _db.SaveChangesAsync();
        if (oldBlob is not null) await _blobs.DeleteAsync(oldBlob);

        return Ok(new ImageUploadResponse($"/api/users/{userId}/picture", "Profile picture uploaded."));
    }

    /// <summary>Serves a user's profile picture (read from Azure Blob Storage). Logged-in users only.</summary>
    /// <response code="200">The image file.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">The user has no picture.</response>
    [HttpGet("/api/users/{id:int}/picture")]
    [Authorize]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPicture(int id)
    {
        var blobName = await _db.Users.Where(u => u.UserId == id).Select(u => u.ProfilePictureBlob).FirstOrDefaultAsync();
        if (blobName is null) return NotFound();

        var blob = await _blobs.DownloadAsync(blobName);
        if (blob is null) return NotFound();
        return File(blob.Value.Content, blob.Value.ContentType);
    }
}
