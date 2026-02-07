using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ExamsController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/exams")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Exam>>>> GetPublic([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Exam>>.Ok(await db.Exams.Where(x => x.IsActive).OrderByDescending(x => x.Date).ToListAsync()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/exams")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Exam>>>> GetAdmin([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Exam>>.Ok(await db.Exams.OrderByDescending(x => x.Date).ToListAsync()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/exams")]
    public async Task<ActionResult<ApiResponse<Exam>>> Create([FromBody] Exam request, [FromServices] AppDbContext db)
    {
        db.Exams.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<Exam>.Ok(request, "Examen creado"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/exams/{id:int}")]
    public async Task<ActionResult<ApiResponse<Exam>>> Update(int id, [FromBody] Exam request, [FromServices] AppDbContext db)
    {
        var current = await db.Exams.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Exam>.Fail("No existe el examen"));
        current.Title = request.Title;
        current.Description = request.Description;
        current.Date = request.Date;
        current.FileUrl = request.FileUrl;
        current.CategoryId = request.CategoryId;
        current.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<Exam>.Ok(current, "Examen actualizado"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/exams/{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var current = await db.Exams.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound();
        db.Exams.Remove(current);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
