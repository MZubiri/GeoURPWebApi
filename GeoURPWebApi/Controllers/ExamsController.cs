using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ExamsController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/exams")]
    public ActionResult<ApiResponse<IEnumerable<Exam>>> GetPublic([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<Exam>>.Ok(store.Exams.Where(x => x.IsActive).OrderByDescending(x => x.Date).ToList()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/exams")]
    public ActionResult<ApiResponse<IEnumerable<Exam>>> GetAdmin([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<Exam>>.Ok(store.Exams.OrderByDescending(x => x.Date).ToList()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/exams")]
    public ActionResult<ApiResponse<Exam>> Create([FromBody] Exam request, [FromServices] InMemoryStore store)
    {
        request.Id = NextId(store.Exams.Select(x => x.Id)); store.Exams.Add(request);
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<Exam>.Ok(request, "Examen creado"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/exams/{id:int}")]
    public ActionResult<ApiResponse<Exam>> Update(int id, [FromBody] Exam request, [FromServices] InMemoryStore store)
    {
        var current = store.Exams.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Exam>.Fail("No existe el examen"));
        current.Title = request.Title; current.Description = request.Description; current.Date = request.Date; current.FileUrl = request.FileUrl; current.CategoryId = request.CategoryId; current.IsActive = request.IsActive;
        return Ok(ApiResponse<Exam>.Ok(current, "Examen actualizado"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/exams/{id:int}")]
    public ActionResult Delete(int id, [FromServices] InMemoryStore store)
    {
        var current = store.Exams.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound();
        store.Exams.Remove(current); return NoContent();
    }

    private static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}