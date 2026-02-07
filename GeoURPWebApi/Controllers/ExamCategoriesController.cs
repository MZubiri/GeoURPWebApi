using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/exam-categories")]
[Authorize(Roles = "Admin,Editor")]
public sealed class ExamCategoriesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ExamCategory>>>> GetAll([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<ExamCategory>>.Ok(await db.ExamCategories.OrderBy(x => x.Id).ToListAsync()));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExamCategory>>> Create([FromBody] ExamCategory request, [FromServices] AppDbContext db)
    {
        db.ExamCategories.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), ApiResponse<ExamCategory>.Ok(request, "Categoría creada"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ExamCategory>>> Update(int id, [FromBody] ExamCategory request, [FromServices] AppDbContext db)
    {
        var c = await db.ExamCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound(ApiResponse<ExamCategory>.Fail("No existe la categoría"));
        c.Name = request.Name;
        c.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<ExamCategory>.Ok(c, "Categoría actualizada"));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var c = await db.ExamCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        db.ExamCategories.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
