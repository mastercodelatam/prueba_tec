using Microsoft.AspNetCore.Mvc;
using MockApi.Models;
using MockApi.Services;

namespace MockApi.Controllers;

[ApiController]
[Route("oauth")]
public class OAuthController : ControllerBase
{
    private readonly TokenService _tokenService;

    public OAuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("token")]
    public IActionResult GetToken([FromForm] TokenRequest request)
    {
        if (request.GrantType != "client_credentials")
        {
            return BadRequest(new { error = "unsupported_grant_type" });
        }

        var (isValid, token, expiresIn) = _tokenService.GenerateToken(request.ClientId, request.ClientSecret);

        if (!isValid)
        {
            return Unauthorized(new { error = "invalid_client" });
        }

        return Ok(new TokenResponse
        {
            AccessToken = token!,
            TokenType = "Bearer",
            ExpiresIn = expiresIn
        });
    }
}
