using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/research-categories")]
[Authorize(Roles = "Admin,Editor")]
public sealed class ResearchCategoriesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<ResearchCategory>>>> GetAll([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<ResearchCategory>>.Ok(await db.ResearchCategories.OrderBy(x => x.Id).ToListAsync()));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ResearchCategory>>> Create([FromBody] ResearchCategory request, [FromServices] AppDbContext db)
    {
        db.ResearchCategories.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), ApiResponse<ResearchCategory>.Ok(request, "Categoría creada"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<ResearchCategory>>> Update(int id, [FromBody] ResearchCategory request, [FromServices] AppDbContext db)
    {
        var c = await db.ResearchCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound(ApiResponse<ResearchCategory>.Fail("No existe la categoría"));
        c.Name = request.Name;
        c.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<ResearchCategory>.Ok(c, "Categoría actualizada"));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var c = await db.ResearchCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        db.ResearchCategories.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
