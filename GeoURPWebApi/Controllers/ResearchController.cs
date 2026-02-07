using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ResearchController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/research")]
    public ActionResult<ApiResponse<IEnumerable<Research>>> GetPublic([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<Research>>.Ok(store.Researches.Where(x => x.IsActive).OrderByDescending(x => x.PublishedAt).ToList()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/research")]
    public ActionResult<ApiResponse<IEnumerable<Research>>> GetAdmin([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<Research>>.Ok(store.Researches.OrderByDescending(x => x.PublishedAt).ToList()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/research")]
    public ActionResult<ApiResponse<Research>> Create([FromBody] Research request, [FromServices] InMemoryStore store)
    {
        request.Id = NextId(store.Researches.Select(x => x.Id)); store.Researches.Add(request);
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<Research>.Ok(request, "Investigación creada"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/research/{id:int}")]
    public ActionResult<ApiResponse<Research>> Update(int id, [FromBody] Research request, [FromServices] InMemoryStore store)
    {
        var current = store.Researches.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Research>.Fail("No existe la investigación"));
        current.Title = request.Title; current.Summary = request.Summary; current.FileUrl = request.FileUrl; current.CategoryId = request.CategoryId; current.PublishedAt = request.PublishedAt; current.IsActive = request.IsActive;
        return Ok(ApiResponse<Research>.Ok(current, "Investigación actualizada"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/research/{id:int}")]
    public ActionResult Delete(int id, [FromServices] InMemoryStore store)
    {
        var current = store.Researches.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound();
        store.Researches.Remove(current); return NoContent();
    }

    private static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}