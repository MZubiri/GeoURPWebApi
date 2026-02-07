using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ResearchController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/research")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Research>>>> GetPublic([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Research>>.Ok(await db.Researches.Where(x => x.IsActive).OrderByDescending(x => x.PublishedAt).ToListAsync()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/research")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Research>>>> GetAdmin([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Research>>.Ok(await db.Researches.OrderByDescending(x => x.PublishedAt).ToListAsync()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/research")]
    public async Task<ActionResult<ApiResponse<Research>>> Create([FromBody] Research request, [FromServices] AppDbContext db)
    {
        db.Researches.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<Research>.Ok(request, "Investigación creada"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/research/{id:int}")]
    public async Task<ActionResult<ApiResponse<Research>>> Update(int id, [FromBody] Research request, [FromServices] AppDbContext db)
    {
        var current = await db.Researches.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Research>.Fail("No existe la investigación"));
        current.Title = request.Title;
        current.Summary = request.Summary;
        current.FileUrl = request.FileUrl;
        current.CategoryId = request.CategoryId;
        current.PublishedAt = request.PublishedAt;
        current.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<Research>.Ok(current, "Investigación actualizada"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/research/{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var current = await db.Researches.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound();
        db.Researches.Remove(current);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
