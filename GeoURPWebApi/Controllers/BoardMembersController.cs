using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class BoardMembersController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/board-members")]
    public ActionResult<ApiResponse<IEnumerable<BoardMember>>> GetPublic([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<BoardMember>>.Ok(store.BoardMembers.Where(x => x.IsActive).OrderBy(x => x.SortOrder).ToList()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/board-members")]
    public ActionResult<ApiResponse<IEnumerable<BoardMember>>> GetAdmin([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<BoardMember>>.Ok(store.BoardMembers.OrderBy(x => x.SortOrder).ToList()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/board-members")]
    public ActionResult<ApiResponse<BoardMember>> Create([FromBody] BoardMember request, [FromServices] InMemoryStore store)
    {
        request.Id = NextId(store.BoardMembers.Select(x => x.Id));
        store.BoardMembers.Add(request);
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<BoardMember>.Ok(request, "Directivo creado"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/board-members/{id:int}")]
    public ActionResult<ApiResponse<BoardMember>> Update(int id, [FromBody] BoardMember request, [FromServices] InMemoryStore store)
    {
        var current = store.BoardMembers.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<BoardMember>.Fail("No existe el directivo"));
        current.FullName = request.FullName; current.Position = request.Position; current.PhotoUrl = request.PhotoUrl; current.Bio = request.Bio; current.SortOrder = request.SortOrder; current.IsActive = request.IsActive;
        return Ok(ApiResponse<BoardMember>.Ok(current, "Directivo actualizado"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/board-members/{id:int}")]
    public ActionResult Delete(int id, [FromServices] InMemoryStore store)
    {
        var current = store.BoardMembers.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound();
        store.BoardMembers.Remove(current); return NoContent();
    }

    private static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}