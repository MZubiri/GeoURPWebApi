using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using GeoURPWebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        [FromServices] AppDbContext db,
        [FromServices] JwtTokenService jwt,
        [FromServices] PasswordService passwordService,
        [FromServices] AccessControlService accessControlService)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await db.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail && x.IsActive);

        if (user is null || !passwordService.VerifyPassword(request.Password, user.Password))
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail("Credenciales invalidas"));
        }

        user.Roles = user.UserRoles.Select(x => x.Role.Name).ToList();

        var canAccessMembers = await accessControlService.CanAccessMembersAsync(user.Email, db);
        var canAccessUsers = !string.IsNullOrWhiteSpace(user.Position) || user.UserRoles.Any(ur => ur.Role.Name == "Admin");
        var canApproveRegistrations = await accessControlService.CanApproveRegistrationsAsync(user.Email, db);

        return Ok(ApiResponse<LoginResponse>.Ok(
            jwt.Generate(user, canAccessMembers, canAccessUsers, canApproveRegistrations),
            "Login exitoso"));
    }

    [AllowAnonymous]
    [HttpPost("setup-admin")]
    public async Task<ActionResult<ApiResponse<User>>> SetupAdmin(
        [FromServices] AppDbContext db,
        [FromServices] PasswordService passwordService)
    {
        if (await db.Users.AnyAsync())
        {
            return BadRequest(ApiResponse<User>.Fail("Ya existen usuarios en el sistema"));
        }

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole is null)
        {
            adminRole = new Role { Name = "Admin" };
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();
        }

        var adminUser = new User
        {
            Name = "Administrador",
            Email = "admin@geourp.com",
            Password = passwordService.HashPassword("Admin123!"),
            IsActive = true
        };

        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        db.UserRoles.Add(new UserRole
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });
        await db.SaveChangesAsync();

        adminUser.Roles = new List<string> { "Admin" };
        return Ok(ApiResponse<User>.Ok(adminUser, "Usuario administrador creado exitosamente"));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] AppDbContext db,
        [FromServices] PasswordService passwordService)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            return Unauthorized(ApiResponse<string>.Fail("Usuario no autenticado"));
        }

        var currentUserId = int.Parse(userIdClaim);

        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(ApiResponse<string>.Fail("Las contrasenas no coinciden"));
        }

        if (request.NewPassword.Length < 6)
        {
            return BadRequest(ApiResponse<string>.Fail("La contrasena debe tener al menos 6 caracteres"));
        }

        var user = await db.Users.FindAsync(currentUserId);
        if (user == null)
        {
            return NotFound(ApiResponse<string>.Fail("Usuario no encontrado"));
        }

        user.Password = passwordService.HashPassword(request.NewPassword);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok(string.Empty, "Nueva contrasena guardada correctamente."));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<string>>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] AppDbContext db,
        [FromServices] PasswordService passwordService,
        [FromServices] EmailService emailService)
    {
        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(ApiResponse<string>.Fail("Ingresa tu correo registrado."));
        }

        const string recoveryMessage = "Si el correo esta registrado, recibiras una contrasena temporal.";

        var normalizedEmail = email.ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail && x.IsActive);
        if (user is null)
        {
            return Ok(ApiResponse<string>.Ok(string.Empty, recoveryMessage));
        }

        var temporaryPassword = passwordService.GenerateTemporaryPassword();
        var sent = await emailService.SendPasswordRecoveryAsync(user.Email, user.Name, temporaryPassword);
        if (!sent)
        {
            return StatusCode(500, ApiResponse<string>.Fail(
                "No se pudo enviar el correo de recuperacion. Intenta nuevamente."));
        }

        user.Password = passwordService.HashPassword(temporaryPassword);
        await db.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok(string.Empty, recoveryMessage));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<string>>> Register(
        [FromBody] RegisterRequest request,
        [FromServices] AppDbContext db,
        [FromServices] PasswordService passwordService)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var name = request.Name.Trim();
        var phone = request.Phone.Trim();
        var password = request.Password.Trim();
        var major = request.Major.Trim();
        var cycle = request.Cycle.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(major) || string.IsNullOrWhiteSpace(cycle))
        {
            return BadRequest(ApiResponse<string>.Fail("Todos los campos son requeridos."));
        }

        if (!email.EndsWith("@urp.edu.pe", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(ApiResponse<string>.Fail("Debes registrarte con tu correo institucional @urp.edu.pe."));
        }

        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (existingUser != null)
        {
            return BadRequest(ApiResponse<string>.Fail("El correo institucional ya se encuentra registrado."));
        }

        var invitadoRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Invitado");
        if (invitadoRole == null)
        {
            invitadoRole = new Role { Name = "Invitado" };
            db.Roles.Add(invitadoRole);
            await db.SaveChangesAsync();
        }

        var user = new User
        {
            Name = name,
            Email = email,
            Password = passwordService.HashPassword(password),
            Phone = phone,
            Major = major,
            Cycle = cycle,
            IsActive = false // Requiere aprobación
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = invitadoRole.Id });
        await db.SaveChangesAsync();

        return Ok(ApiResponse<string>.Ok(string.Empty, "Registro exitoso. Tu cuenta se encuentra en proceso de aprobación por los directores."));
    }

    [Authorize]
    [HttpGet("my-profile")]
    public async Task<ActionResult<ApiResponse<User>>> GetMyProfile(
        [FromServices] AppDbContext db)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            return Unauthorized(ApiResponse<User>.Fail("Usuario no autenticado"));
        }
        var userId = int.Parse(userIdClaim);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return NotFound(ApiResponse<User>.Fail("Usuario no encontrado"));
        }

        return Ok(ApiResponse<User>.Ok(user));
    }

    [Authorize]
    [HttpPut("my-profile")]
    public async Task<ActionResult<ApiResponse<User>>> UpdateProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] AppDbContext db)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            return Unauthorized(ApiResponse<User>.Fail("Usuario no autenticado"));
        }
        var userId = int.Parse(userIdClaim);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return NotFound(ApiResponse<User>.Fail("Usuario no encontrado"));
        }

        var name = request.Name.Trim();
        var phone = request.Phone.Trim();
        var major = request.Major.Trim();
        var cycle = request.Cycle.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone) ||
            string.IsNullOrWhiteSpace(major) || string.IsNullOrWhiteSpace(cycle))
        {
            return BadRequest(ApiResponse<User>.Fail("Nombre, teléfono, carrera y ciclo son requeridos."));
        }

        user.Name = name;
        user.Phone = phone;
        user.Major = major;
        user.Cycle = cycle;
        user.Birthday = string.IsNullOrWhiteSpace(request.Birthday) ? null : request.Birthday.Trim();
        user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        user.PhotoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl.Trim();
        user.LinkedInUrl = string.IsNullOrWhiteSpace(request.LinkedInUrl) ? null : request.LinkedInUrl.Trim();

        await db.SaveChangesAsync();
        return Ok(ApiResponse<User>.Ok(user, "Perfil actualizado exitosamente."));
    }

    [Authorize]
    [HttpPost("my-profile/upload-photo")]
    public async Task<ActionResult<ApiResponse<UploadPhotoResponseDto>>> UploadMyPhoto(
        IFormFile file,
        [FromServices] IWebHostEnvironment env,
        [FromServices] ILogger<AuthController> logger)
    {
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
            return BadRequest(ApiResponse<UploadPhotoResponseDto>.Fail("Solo se permiten imágenes (JPEG, PNG, WEBP)"));
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
            var uploadsFolder = Path.Combine(env.WebRootPath, "uploads", "profile-photos");

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
                Url = $"/uploads/profile-photos/{fileName}",
                FileName = fileName
            };

            return Ok(ApiResponse<UploadPhotoResponseDto>.Ok(response, "Foto de perfil subida exitosamente"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al subir foto de perfil");
            return StatusCode(500, ApiResponse<UploadPhotoResponseDto>.Fail("Error interno del servidor."));
        }
    }
}

public record RegisterRequest(string Name, string Email, string Phone, string Password, string Major, string Cycle);
public record UpdateProfileRequest(string Name, string Phone, string Major, string Cycle, string? Birthday, string? Bio, string? PhotoUrl, string? LinkedInUrl);
