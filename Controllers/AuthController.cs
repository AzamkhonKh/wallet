namespace WalletNet.Controllers;

using WalletNet.Models;
using Microsoft.AspNetCore.Mvc;
using WalletNet.Services;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TokenService _tokenService;

    public AuthController(ApplicationDbContext dbContext, TokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = _dbContext.Users.FirstOrDefault(u => u.Username == request.Username);

        if (user == null || !VerifyPassword(request.Password, user.Password))
        {
            return Unauthorized("Invalid credentials");
        }

        // Get client metadata
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

        // Generate token and store it
        var token = await _tokenService.GenerateTokenAsync(user, clientIp, userAgent);

        return Ok(new { Token = token });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RevokeTokenRequest request)
    {
        var success = await _tokenService.RevokeTokenAsync(request.Token);
        if (!success)
            return BadRequest("Invalid token");

        return Ok("Token revoked");
    }

    private bool VerifyPassword(string inputPassword, string storedHash)
    {
        // Implement password verification logic here
        return inputPassword == storedHash;
    }
}

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class RevokeTokenRequest
{
    public string Token { get; set; }
}
