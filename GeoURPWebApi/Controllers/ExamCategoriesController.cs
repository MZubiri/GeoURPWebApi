using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/exam-categories")]
[Authorize(Roles = "Admin,Editor")]
public sealed class ExamCategoriesController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<IEnumerable<ExamCategory>>> GetAll([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<ExamCategory>>.Ok(store.ExamCategories.OrderBy(x => x.Id).ToList()));
    [HttpPost]
    public ActionResult<ApiResponse<ExamCategory>> Create([FromBody] ExamCategory request, [FromServices] InMemoryStore store)
    { request.Id = NextId(store.ExamCategories.Select(x => x.Id)); store.ExamCategories.Add(request); return CreatedAtAction(nameof(GetAll), ApiResponse<ExamCategory>.Ok(request, "Categoría creada")); }
    [HttpPut("{id:int}")]
    public ActionResult<ApiResponse<ExamCategory>> Update(int id, [FromBody] ExamCategory request, [FromServices] InMemoryStore store)
    { var c = store.ExamCategories.FirstOrDefault(x => x.Id == id); if (c is null) return NotFound(ApiResponse<ExamCategory>.Fail("No existe la categoría")); c.Name = request.Name; c.IsActive = request.IsActive; return Ok(ApiResponse<ExamCategory>.Ok(c, "Categoría actualizada")); }
    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id, [FromServices] InMemoryStore store)
    { var c = store.ExamCategories.FirstOrDefault(x => x.Id == id); if (c is null) return NotFound(); store.ExamCategories.Remove(c); return NoContent(); }
    private static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}