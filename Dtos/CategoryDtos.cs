using System.ComponentModel.DataAnnotations;

namespace RaceDay_api.Dtos;

public class CategoryRequest
{
    // <example>21km Half Marathon</example>
    [Required, StringLength(100)] public required string CategoryName { get; set; }
    /// <example>250.00</example>
    [Range(typeof(decimal), "0", "100000")] public required decimal EntryFee { get; set; }
    /// <summary>Optional entry limit. Leave out for unlimited.</summary>
    [Range(1, int.MaxValue)] public int? MaxParticipants { get; set; }

}

public record CategoryResponse(
    int CategoryId, int EventId, string CategoryName, decimal EntryFee, int? MaxParticipants, int RegisteredCount);
