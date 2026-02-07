using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class BoardMembersController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/board-members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BoardMember>>>> GetPublic([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<BoardMember>>.Ok(await db.BoardMembers.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToListAsync()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/board-members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BoardMember>>>> GetAdmin([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<BoardMember>>.Ok(await db.BoardMembers.OrderBy(x => x.SortOrder).ToListAsync()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/board-members")]
    public async Task<ActionResult<ApiResponse<BoardMember>>> Create([FromBody] BoardMember request, [FromServices] AppDbContext db)
    {
        db.BoardMembers.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<BoardMember>.Ok(request, "Directivo creado"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/board-members/{id:int}")]
    public async Task<ActionResult<ApiResponse<BoardMember>>> Update(int id, [FromBody] BoardMember request, [FromServices] AppDbContext db)
    {
        var current = await db.BoardMembers.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<BoardMember>.Fail("No existe el directivo"));
        current.FullName = request.FullName;
        current.Position = request.Position;
        current.PhotoUrl = request.PhotoUrl;
        current.Bio = request.Bio;
        current.SortOrder = request.SortOrder;
        current.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<BoardMember>.Ok(current, "Directivo actualizado"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/board-members/{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var current = await db.BoardMembers.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound();
        db.BoardMembers.Remove(current);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
