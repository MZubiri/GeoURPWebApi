using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using GeoURPWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class BoardMembersController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/board-members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<User>>>> GetPublic([FromServices] AppDbContext db)
    {
        var members = await db.Users
            .Where(x => x.IsActive && x.Position != null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<User>>.Ok(members));
    }

    [Authorize]
    [HttpGet("admin/board-members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<User>>>> GetAdmin(
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessMembersAsync(User, db))
        {
            return ForbiddenResponse<IEnumerable<User>>("Solo los directores de rangos altos pueden acceder a la gestión de directivos.");
        }

        var members = await db.Users
            .Where(x => x.Position != null)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<User>>.Ok(members));
    }

    [Authorize]
    [HttpPost("admin/board-members/upload-photo")]
    public async Task<ActionResult<ApiResponse<UploadPhotoResponseDto>>> UploadPhoto(
        IFormFile file,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService,
        [FromServices] IWebHostEnvironment env,
        [FromServices] ILogger<BoardMembersController> logger)
    {
        if (!await accessControlService.CanAccessMembersAsync(User, db))
        {
            return ForbiddenResponse<UploadPhotoResponseDto>("Solo los directores autorizados pueden gestionar fotos.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<UploadPhotoResponseDto>.Fail("El archivo es requerido"));
        }

        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return BadRequest(ApiResponse<UploadPhotoResponseDto>.Fail("El archivo no puede superar los 5 MB"));
        }

        var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedContentTypes.Contains(file.ContentType.ToLower()))
        {
            return BadRequest(ApiResponse<UploadPhotoResponseDto>.Fail(
                "Solo se permiten archivos de imagen (JPEG, PNG, WEBP)"));
        }

        try
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
            {
                extension = file.ContentType.ToLower() switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    _ => ".jpg"
                };
            }

            var fileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "board-members");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var response = new UploadPhotoResponseDto
            {
                Url = $"/uploads/board-members/{fileName}",
                FileName = fileName
            };

            return Ok(ApiResponse<UploadPhotoResponseDto>.Ok(response, "Foto subida exitosamente"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al subir la foto del miembro");
            return StatusCode(500, ApiResponse<UploadPhotoResponseDto>.Fail(
                "Error interno al procesar la imagen. Intente nuevamente."));
        }
    }

    [Authorize]
    [HttpPost("admin/board-members")]
    public async Task<ActionResult<ApiResponse<User>>> Create(
        [FromBody] User request,
        [FromServices] AppDbContext db,
        [FromServices] PasswordService passwordService,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessMembersAsync(User, db))
        {
            return ForbiddenResponse<User>("Solo los directores autorizados pueden gestionar miembros.");
        }

        NormalizeBoardMember(request);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(ApiResponse<User>.Fail("El email es requerido"));
        }

        if (!IsBirthdayValid(request.Birthday))
        {
            return BadRequest(ApiResponse<User>.Fail("El cumpleaños debe tener formato DD/MM."));
        }

        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (existingUser != null)
        {
            return BadRequest(ApiResponse<User>.Fail("Ya existe un usuario con ese email"));
        }

        var invitadoRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Invitado");
        if (invitadoRole == null)
        {
            invitadoRole = new Role { Name = "Invitado" };
            db.Roles.Add(invitadoRole);
            await db.SaveChangesAsync();
        }

        var temporaryPassword = passwordService.GenerateTemporaryPassword();

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Password = passwordService.HashPassword(temporaryPassword),
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

        db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = invitadoRole.Id
        });
        await db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetAdmin),
            ApiResponse<User>.Ok(user, $"Directivo creado exitosamente. Contraseña temporal: {temporaryPassword}"));
    }

    [Authorize]
    [HttpPut("admin/board-members/{id:int}")]
    public async Task<ActionResult<ApiResponse<User>>> Update(
        int id,
        [FromBody] User request,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessMembersAsync(User, db))
        {
            return ForbiddenResponse<User>("Solo los directores autorizados pueden gestionar miembros.");
        }

        NormalizeBoardMember(request);

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(ApiResponse<User>.Fail("El email es requerido"));
        }

        if (!IsBirthdayValid(request.Birthday))
        {
            return BadRequest(ApiResponse<User>.Fail("El cumpleaños debe tener formato DD/MM."));
        }

        var current = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null)
        {
            return NotFound(ApiResponse<User>.Fail("No existe el directivo"));
        }

        var duplicatedUser = await db.Users.FirstOrDefaultAsync(u =>
            u.Email.ToLower() == request.Email.ToLower() && u.Id != id);
        if (duplicatedUser != null)
        {
            return BadRequest(ApiResponse<User>.Fail("Ya existe otro usuario con ese email"));
        }

        current.Name = request.Name;
        current.Email = request.Email;
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
        current.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return Ok(ApiResponse<User>.Ok(current, "Directivo actualizado"));
    }

    [Authorize]
    [HttpPatch("admin/board-members/{id:int}/activate")]
    public async Task<ActionResult<ApiResponse<User>>> Activate(
        int id,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessMembersAsync(User, db))
        {
            return ForbiddenResponse<User>("Solo los directores autorizados pueden gestionar miembros.");
        }

        var current = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null)
        {
            return NotFound(ApiResponse<User>.Fail("No existe el directivo"));
        }

        current.IsActive = true;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<User>.Ok(current, "Directivo activado"));
    }

    [Authorize]
    [HttpPatch("admin/board-members/{id:int}/deactivate")]
    public async Task<ActionResult<ApiResponse<User>>> Deactivate(
        int id,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessMembersAsync(User, db))
        {
            return ForbiddenResponse<User>("Solo los directores autorizados pueden gestionar miembros.");
        }

        var current = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null)
        {
            return NotFound(ApiResponse<User>.Fail("No existe el directivo"));
        }

        current.IsActive = false;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<User>.Ok(current, "Directivo desactivado"));
    }

    [Authorize]
    [HttpDelete("admin/board-members/{id:int}")]
    public async Task<ActionResult> Delete(
        int id,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!await accessControlService.CanAccessMembersAsync(User, db))
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<string>.Fail("Solo los directores autorizados pueden gestionar miembros."));
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

    private static void NormalizeBoardMember(User member)
    {
        member.Name = member.Name.Trim();
        member.Email = member.Email.Trim();
        member.Position = string.IsNullOrWhiteSpace(member.Position) ? null : member.Position.Trim();
        member.Code = string.IsNullOrWhiteSpace(member.Code) ? null : member.Code.Trim();
        member.Birthday = string.IsNullOrWhiteSpace(member.Birthday) ? null : member.Birthday.Trim();
        member.PhotoUrl = string.IsNullOrWhiteSpace(member.PhotoUrl) ? null : member.PhotoUrl.Trim();
        member.Bio = string.IsNullOrWhiteSpace(member.Bio) ? null : member.Bio.Trim();
        member.LinkedInUrl = string.IsNullOrWhiteSpace(member.LinkedInUrl) ? null : member.LinkedInUrl.Trim();
    }

    private static bool IsBirthdayValid(string? birthday)
    {
        if (string.IsNullOrWhiteSpace(birthday))
        {
            return true;
        }

        return Regex.IsMatch(birthday.Trim(), "^(0[1-9]|[12][0-9]|3[01])/(0[1-9]|1[0-2])$");
    }

    private ActionResult<ApiResponse<T>> ForbiddenResponse<T>(string message)
    {
        return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail(message));
    }
}
