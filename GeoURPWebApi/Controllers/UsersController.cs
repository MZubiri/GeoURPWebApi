using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<User>>>> GetAll([FromServices] AppDbContext db)
    {
        var users = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .OrderBy(x => x.Id)
            .ToListAsync();

        foreach (var u in users)
        {
            u.Roles = u.UserRoles.Select(x => x.Role.Name).ToList();
        }

        return Ok(ApiResponse<IEnumerable<User>>.Ok(users));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<User>>> Create([FromBody] User request, [FromServices] AppDbContext db)
    {
        var roleIds = await db.Roles.Where(r => request.Roles.Contains(r.Name)).Select(r => r.Id).ToListAsync();
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Password = request.Password,
            IsActive = request.IsActive
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        foreach (var roleId in roleIds)
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }

        await db.SaveChangesAsync();
        user.Roles = request.Roles;
        return CreatedAtAction(nameof(GetAll), ApiResponse<User>.Ok(user, "Usuario creado"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<User>>> Update(int id, [FromBody] User request, [FromServices] AppDbContext db)
    {
        var current = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (current is null) return NotFound(ApiResponse<User>.Fail("No existe el usuario"));

        current.Name = request.Name;
        current.Email = request.Email;
        current.Password = request.Password;
        current.IsActive = request.IsActive;

        db.UserRoles.RemoveRange(current.UserRoles);
        var roleIds = await db.Roles.Where(r => request.Roles.Contains(r.Name)).Select(r => r.Id).ToListAsync();
        foreach (var roleId in roleIds)
        {
            db.UserRoles.Add(new UserRole { UserId = current.Id, RoleId = roleId });
        }

        await db.SaveChangesAsync();
        current.Roles = request.Roles;
        return Ok(ApiResponse<User>.Ok(current, "Usuario actualizado"));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var current = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound();
        db.Users.Remove(current);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
