using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/roles")]
[Authorize(Roles = "Admin")]
public sealed class RolesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Role>>>> GetAll([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Role>>.Ok(await db.Roles.OrderBy(x => x.Id).ToListAsync()));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Role>>> Create([FromBody] Role request, [FromServices] AppDbContext db)
    {
        db.Roles.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), ApiResponse<Role>.Ok(request, "Rol creado"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<Role>>> Update(int id, [FromBody] Role request, [FromServices] AppDbContext db)
    {
        var current = await db.Roles.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Role>.Fail("No existe el rol"));
        current.Name = request.Name;
        current.Description = request.Description;
        current.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<Role>.Ok(current, "Rol actualizado"));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var current = await db.Roles.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound();
        db.Roles.Remove(current);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
