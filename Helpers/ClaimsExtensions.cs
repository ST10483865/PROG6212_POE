using System.Security.Claims;

namespace RaceDay_api.Helpers;
//start of namespace


public static class ClaimsExtensions
{//start of class
    public const string ProfileIdClaim = "ProfileId";

    public static int GetUserId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static int GetProfileId(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ProfileIdClaim)!);



}//end of class
