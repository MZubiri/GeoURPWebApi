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
[Route("api/v1/admin/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<User>>>> GetAll(
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessUsersViewAsync(User, db))
        {
            return ForbiddenResponse<IEnumerable<User>>("No tienes permisos para acceder a usuarios.");
        }

        var users = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .OrderBy(x => x.Id)
            .ToListAsync();

        foreach (var user in users)
        {
            user.Roles = user.UserRoles.Select(x => x.Role.Name).ToList();
        }

        return Ok(ApiResponse<IEnumerable<User>>.Ok(users));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<User>>> GetById(
        int id,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessUsersViewAsync(User, db))
        {
            return ForbiddenResponse<User>("No tienes permisos para acceder a usuarios.");
        }

        var user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound(ApiResponse<User>.Fail("Usuario no encontrado"));
        }

        user.Roles = user.UserRoles.Select(x => x.Role.Name).ToList();
        return Ok(ApiResponse<User>.Ok(user));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<User>>> Create(
        [FromBody] User request,
        [FromServices] AppDbContext db,
        [FromServices] PasswordService passwordService,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessUsersViewAsync(User, db))
        {
            return ForbiddenResponse<User>("No tienes permisos para gestionar usuarios.");
        }

        var roleIds = await db.Roles
            .Where(r => request.Roles.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync();

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Password = passwordService.HashPassword(request.Password),
            IsActive = request.IsActive,
            Phone = request.Phone,
            Major = request.Major,
            Cycle = request.Cycle,
            Position = request.Position,
            Code = request.Code,
            Birthday = request.Birthday,
            PhotoUrl = request.PhotoUrl,
            Bio = request.Bio,
            SortOrder = request.SortOrder,
            LinkedInUrl = request.LinkedInUrl
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
    public async Task<ActionResult<ApiResponse<User>>> Update(
        int id,
        [FromBody] User request,
        [FromServices] AppDbContext db,
        [FromServices] PasswordService passwordService,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessUsersViewAsync(User, db))
        {
            return ForbiddenResponse<User>("No tienes permisos para gestionar usuarios.");
        }

        var current = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (current is null)
        {
            return NotFound(ApiResponse<User>.Fail("No existe el usuario"));
        }

        current.Name = request.Name;
        current.Email = request.Email;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            current.Password = passwordService.HashPassword(request.Password);
        }

        current.IsActive = request.IsActive;
        current.Phone = request.Phone;
        current.Major = request.Major;
        current.Cycle = request.Cycle;
        current.Position = request.Position;
        current.Code = request.Code;
        current.Birthday = request.Birthday;
        current.PhotoUrl = request.PhotoUrl;
        current.Bio = request.Bio;
        current.SortOrder = request.SortOrder;
        current.LinkedInUrl = request.LinkedInUrl;

        db.UserRoles.RemoveRange(current.UserRoles);
        var roleIds = await db.Roles
            .Where(r => request.Roles.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync();

        foreach (var roleId in roleIds)
        {
            db.UserRoles.Add(new UserRole { UserId = current.Id, RoleId = roleId });
        }

        await db.SaveChangesAsync();
        current.Roles = request.Roles;
        return Ok(ApiResponse<User>.Ok(current, "Usuario actualizado"));
    }

    [HttpPatch("{id:int}/roles")]
    public async Task<ActionResult<ApiResponse<User>>> UpdateRoles(
        int id,
        [FromBody] UpdateUserRolesRequest request,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessUsersViewAsync(User, db))
        {
            return ForbiddenResponse<User>("No tienes permisos para gestionar usuarios.");
        }

        if (request.Roles == null || request.Roles.Count == 0)
        {
            return BadRequest(ApiResponse<User>.Fail("Debe proporcionar al menos un rol"));
        }

        var user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound(ApiResponse<User>.Fail("Usuario no encontrado"));
        }

        var roleIds = await db.Roles
            .Where(r => request.Roles.Contains(r.Name))
            .Select(r => r.Id)
            .ToListAsync();

        if (roleIds.Count != request.Roles.Count)
        {
            return BadRequest(ApiResponse<User>.Fail("Uno o mas roles no existen"));
        }

        db.UserRoles.RemoveRange(user.UserRoles);

        foreach (var roleId in roleIds)
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = roleId
            });
        }

        await db.SaveChangesAsync();

        user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == id);

        user!.Roles = user.UserRoles.Select(x => x.Role.Name).ToList();
        return Ok(ApiResponse<User>.Ok(user, "Roles actualizados exitosamente"));
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
                ApiResponse<string>.Fail("No tienes permisos para gestionar usuarios."));
        }

        var current = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null)
        {
            return NotFound();
        }

        db.Users.Remove(current);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("pending")]
    public async Task<ActionResult<ApiResponse<IEnumerable<User>>>> GetPending(
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanApproveRegistrationsAsync(User, db))
        {
            return ForbiddenResponse<IEnumerable<User>>("No tienes permisos para ver registros pendientes.");
        }

        var users = await db.Users
            .Where(x => !x.IsActive && x.Position == null)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<User>>.Ok(users));
    }

    [HttpPost("{id:int}/approve")]
    public async Task<ActionResult<ApiResponse<User>>> Approve(
        int id,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService,
        [FromServices] EmailService emailService)
    {
        if (!await accessControlService.CanApproveRegistrationsAsync(User, db))
        {
            return ForbiddenResponse<User>("No tienes permisos para aprobar postulantes.");
        }

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound(ApiResponse<User>.Fail("Postulante no encontrado"));
        }

        if (user.IsActive)
        {
            return BadRequest(ApiResponse<User>.Fail("El usuario ya está activo"));
        }

        user.IsActive = true;
        await db.SaveChangesAsync();

        try
        {
            await emailService.SendWelcomeEmailAsync(user.Email, user.Name);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending welcome email: {ex.Message}");
        }

        return Ok(ApiResponse<User>.Ok(user, "Postulante aprobado exitosamente"));
    }

    [HttpPost("{id:int}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> Reject(
        int id,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanApproveRegistrationsAsync(User, db))
        {
            return ForbiddenResponse<string>("No tienes permisos para rechazar postulantes.");
        }

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound(ApiResponse<string>.Fail("Postulante no encontrado"));
        }

        if (user.IsActive)
        {
            return BadRequest(ApiResponse<string>.Fail("No se puede rechazar un usuario activo."));
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok(string.Empty, "Postulante rechazado y eliminado."));
    }

    private ActionResult<ApiResponse<T>> ForbiddenResponse<T>(string message)
    {
        return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail(message));
    }
}
