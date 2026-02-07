using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ContactController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("public/contact")]
    public async Task<ActionResult<ApiResponse<ContactMessage>>> Create([FromBody] ContactMessage request, [FromServices] AppDbContext db)
    {
        request.CreatedAt = DateTime.UtcNow;
        db.ContactMessages.Add(request);
        await db.SaveChangesAsync();
        return Created(string.Empty, ApiResponse<ContactMessage>.Ok(request, "Mensaje recibido"));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/contact")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ContactMessage>>>> GetAll([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<ContactMessage>>.Ok(await db.ContactMessages.OrderByDescending(x => x.CreatedAt).ToListAsync()));
}
