using Microsoft.AspNetCore.Mvc;

namespace gearOps.Controllers;

/// <summary>
/// Base controller that provides shared helper methods for all API controllers.
/// Eliminates duplication of GetUserIdFromToken across Admin, Staff, Customer, and Auth controllers.
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Extracts the authenticated user's ID from the JWT token claims.
    /// Looks for the "sub" claim first, then falls back to the NameIdentifier claim.
    /// </summary>
    protected int GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst("sub")
                          ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token");

        return userId;
    }
}
