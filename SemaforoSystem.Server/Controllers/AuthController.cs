using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SemaforoSystem.Server.Auth;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("Auth")]
public class AuthController(SignInManager<ApplicationUser> signInManager) : ControllerBase
{
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return NoContent();
    }
}
