using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/roles")]
[Authorize(Roles = "Admin")]
public sealed class RolesController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<IEnumerable<Role>>> GetAll([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<Role>>.Ok(store.Roles.OrderBy(x => x.Id).ToList()));

    [HttpPost]
    public ActionResult<ApiResponse<Role>> Create([FromBody] Role request, [FromServices] InMemoryStore store)
    {
        request.Id = NextId(store.Roles.Select(x => x.Id)); store.Roles.Add(request);
        return CreatedAtAction(nameof(GetAll), ApiResponse<Role>.Ok(request, "Rol creado"));
    }

    [HttpPut("{id:int}")]
    public ActionResult<ApiResponse<Role>> Update(int id, [FromBody] Role request, [FromServices] InMemoryStore store)
    {
        var current = store.Roles.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Role>.Fail("No existe el rol"));
        current.Name = request.Name; current.Description = request.Description; current.IsActive = request.IsActive;
        return Ok(ApiResponse<Role>.Ok(current, "Rol actualizado"));
    }

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id, [FromServices] InMemoryStore store)
    {
        var current = store.Roles.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound();
        store.Roles.Remove(current); return NoContent();
    }

    private static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}