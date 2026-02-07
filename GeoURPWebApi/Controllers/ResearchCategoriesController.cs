using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/research-categories")]
[Authorize(Roles = "Admin,Editor")]
public sealed class ResearchCategoriesController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<IEnumerable<ResearchCategory>>> GetAll([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<ResearchCategory>>.Ok(store.ResearchCategories.OrderBy(x => x.Id).ToList()));
    [HttpPost]
    public ActionResult<ApiResponse<ResearchCategory>> Create([FromBody] ResearchCategory request, [FromServices] InMemoryStore store)
    { request.Id = NextId(store.ResearchCategories.Select(x => x.Id)); store.ResearchCategories.Add(request); return CreatedAtAction(nameof(GetAll), ApiResponse<ResearchCategory>.Ok(request, "Categoría creada")); }
    [HttpPut("{id:int}")]
    public ActionResult<ApiResponse<ResearchCategory>> Update(int id, [FromBody] ResearchCategory request, [FromServices] InMemoryStore store)
    { var c = store.ResearchCategories.FirstOrDefault(x => x.Id == id); if (c is null) return NotFound(ApiResponse<ResearchCategory>.Fail("No existe la categoría")); c.Name = request.Name; c.IsActive = request.IsActive; return Ok(ApiResponse<ResearchCategory>.Ok(c, "Categoría actualizada")); }
    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id, [FromServices] InMemoryStore store)
    { var c = store.ResearchCategories.FirstOrDefault(x => x.Id == id); if (c is null) return NotFound(); store.ResearchCategories.Remove(c); return NoContent(); }
    private static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}