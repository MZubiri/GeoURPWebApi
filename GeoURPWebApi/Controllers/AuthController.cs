using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using GeoURPWebApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    public ActionResult<ApiResponse<LoginResponse>> Login([FromBody] LoginRequest request, [FromServices] InMemoryStore store, [FromServices] JwtTokenService jwt)
    {
        var user = store.Users.FirstOrDefault(x => x.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase) && x.Password == request.Password && x.IsActive);
        if (user is null) return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas"));
        return Ok(ApiResponse<LoginResponse>.Ok(jwt.Generate(user), "Login exitoso"));
    }
}