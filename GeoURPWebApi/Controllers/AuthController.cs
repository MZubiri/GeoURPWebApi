using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using GeoURPWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        [FromServices] AppDbContext db,
        [FromServices] JwtTokenService jwt)
    {
        var user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == request.Email.Trim().ToLower() && x.Password == request.Password && x.IsActive);

        if (user is null)
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales inválidas"));
        }

        user.Roles = user.UserRoles.Select(x => x.Role.Name).ToList();
        return Ok(ApiResponse<LoginResponse>.Ok(jwt.Generate(user), "Login exitoso"));
    }
}
