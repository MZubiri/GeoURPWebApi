using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ResearchController : ControllerBase
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".zip" };
    private static readonly string[] AllowedContentTypes = { "application/pdf", "application/zip", "application/x-zip-compressed" };
    private const long MaxFileSize = 500 * 1024 * 1024; // 500MB

    [AllowAnonymous]
    [HttpGet("public/research")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Research>>>> GetPublic([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Research>>.Ok(await db.Researches.Where(x => x.IsActive).OrderByDescending(x => x.PublishedAt).ToListAsync()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/research")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Research>>>> GetAdmin([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Research>>.Ok(await db.Researches.OrderByDescending(x => x.PublishedAt).ToListAsync()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/research/upload-file")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<string>>> UploadFile([FromForm] IFormFile file)
    {
        try
        {
            // Validar que el archivo no sea null
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<string>.Fail("No se ha proporcionado ningún archivo"));
            }

            // Validar tamaño máximo
            if (file.Length > MaxFileSize)
            {
                return BadRequest(ApiResponse<string>.Fail($"El archivo excede el tamaño máximo permitido de {MaxFileSize / 1024 / 1024}MB"));
            }

            // Validar extensión
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<string>.Fail($"Extensión no permitida. Solo se permiten: {string.Join(", ", AllowedExtensions)}"));
            }

            // Validar content-type
            if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(ApiResponse<string>.Fail($"Tipo de contenido no permitido. Solo se permiten: {string.Join(", ", AllowedContentTypes)}"));
            }

            // Crear directorio si no existe
            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "library", "research");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generar nombre único para evitar colisiones
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Guardar archivo
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Retornar ruta pública relativa
            var publicUrl = $"/uploads/library/research/{uniqueFileName}";
            return Ok(ApiResponse<string>.Ok(publicUrl, "Archivo subido correctamente"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail("Error inesperado al subir el archivo", ex.Message));
        }
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/research")]
    public async Task<ActionResult<ApiResponse<Research>>> Create([FromBody] Research request, [FromServices] AppDbContext db)
    {
        db.Researches.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<Research>.Ok(request, "Investigación creada"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/research/{id:int}")]
    public async Task<ActionResult<ApiResponse<Research>>> Update(int id, [FromBody] Research request, [FromServices] AppDbContext db)
    {
        var current = await db.Researches.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Research>.Fail("No existe la investigación"));
        current.Title = request.Title;
        current.Summary = request.Summary;
        current.FileUrl = request.FileUrl;
        current.CategoryId = request.CategoryId;
        current.PublishedAt = request.PublishedAt;
        current.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<Research>.Ok(current, "Investigación actualizada"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/research/{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var current = await db.Researches.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound();
        db.Researches.Remove(current);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
