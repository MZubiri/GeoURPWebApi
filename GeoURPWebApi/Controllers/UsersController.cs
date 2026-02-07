using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class UsersController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<IEnumerable<User>>> GetAll([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<User>>.Ok(store.Users.OrderBy(x => x.Id).ToList()));

    [HttpPost]
    public ActionResult<ApiResponse<User>> Create([FromBody] User request, [FromServices] InMemoryStore store)
    {
        request.Id = NextId(store.Users.Select(x => x.Id)); store.Users.Add(request);
        return CreatedAtAction(nameof(GetAll), ApiResponse<User>.Ok(request, "Usuario creado"));
    }

    [HttpPut("{id:int}")]
    public ActionResult<ApiResponse<User>> Update(int id, [FromBody] User request, [FromServices] InMemoryStore store)
    {
        var current = store.Users.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<User>.Fail("No existe el usuario"));
        current.Name = request.Name; current.Email = request.Email; current.Password = request.Password; current.IsActive = request.IsActive; current.Roles = request.Roles;
        return Ok(ApiResponse<User>.Ok(current, "Usuario actualizado"));
    }

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id, [FromServices] InMemoryStore store)
    {
        var current = store.Users.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound();
        store.Users.Remove(current); return NoContent();
    }

    private static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}