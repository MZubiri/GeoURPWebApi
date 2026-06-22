using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using GeoURPWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/roles")]
[Authorize]
public sealed class RolesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<Role>>>> GetAll(
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessUsersViewAsync(User, db))
        {
            return ForbiddenResponse<IEnumerable<Role>>("No tienes permisos para acceder a roles.");
        }

        var roles = await db.Roles.OrderBy(r => r.Name).ToListAsync();
        return Ok(ApiResponse<IEnumerable<Role>>.Ok(roles));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Role>>> Create(
        [FromBody] Role request,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessUsersViewAsync(User, db))
        {
            return ForbiddenResponse<Role>("No tienes permisos para gestionar roles.");
        }

        var existingRole = await db.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == request.Name.ToLower());
        if (existingRole != null)
        {
            return BadRequest(ApiResponse<Role>.Fail("Ya existe un rol con ese nombre"));
        }

        db.Roles.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), ApiResponse<Role>.Ok(request, "Rol creado"));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(
        int id,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessUsersViewAsync(User, db))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<string>.Fail("No tienes permisos para gestionar roles."));
        }

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id);
        if (role is null)
        {
            return NotFound();
        }

        var hasUsers = await db.UserRoles.AnyAsync(ur => ur.RoleId == id);
        if (hasUsers)
        {
            return BadRequest(ApiResponse<Role>.Fail("No se puede eliminar el rol porque tiene usuarios asignados"));
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private ActionResult<ApiResponse<T>> ForbiddenResponse<T>(string message)
    {
        return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail(message));
    }
}
