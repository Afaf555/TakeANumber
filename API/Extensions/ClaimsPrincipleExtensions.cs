using System.Security.Claims;

namespace API.Extensions;

public static class ClaimsPrincipleExtensions
{
    public static int GetUserId(this ClaimsPrincipal user)
    {
        return int.Parse(user.FindFirstValue("nameid")
            ?? throw new Exception("Cannot get user id from token"));
    }
}