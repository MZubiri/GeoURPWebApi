using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class BooksController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/books")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Book>>>> GetPublic([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Book>>.Ok(await db.Books.Where(x => x.IsActive).OrderByDescending(x => x.Year).ToListAsync()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/books")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Book>>>> GetAdmin([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Book>>.Ok(await db.Books.OrderByDescending(x => x.Year).ToListAsync()));

    private static readonly string[] AllowedExtensions = { ".pdf", ".zip" };
    private static readonly string[] AllowedContentTypes = { "application/pdf", "application/zip", "application/x-zip-compressed" };
    private const long MaxFileSize = 500 * 1024 * 1024; // 500MB

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/books/upload-file")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<string>>> UploadFile([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<string>.Fail("No se ha proporcionado ningún archivo"));
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest(ApiResponse<string>.Fail($"El archivo excede el tamaño máximo permitido de {MaxFileSize / 1024 / 1024}MB"));
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(ApiResponse<string>.Fail($"Extensión no permitida. Solo se permiten: {string.Join(", ", AllowedExtensions)}"));
            }

            if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(ApiResponse<string>.Fail($"Tipo de contenido no permitido. Solo se permiten: {string.Join(", ", AllowedContentTypes)}"));
            }

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "library", "books");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/library/books/{uniqueFileName}";
            return Ok(ApiResponse<string>.Ok(publicUrl, "Archivo subido correctamente"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail("Error inesperado al subir el archivo", ex.Message));
        }
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/books")]
    public async Task<ActionResult<ApiResponse<Book>>> Create([FromBody] Book request, [FromServices] AppDbContext db)
    {
        db.Books.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<Book>.Ok(request, "Libro creado"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/books/{id:int}")]
    public async Task<ActionResult<ApiResponse<Book>>> Update(int id, [FromBody] Book request, [FromServices] AppDbContext db)
    {
        var current = await db.Books.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Book>.Fail("No existe el libro"));
        current.Title = request.Title;
        current.Author = request.Author;
        current.Editorial = request.Editorial;
        current.Year = request.Year;
        current.FileUrl = request.FileUrl;
        current.CategoryId = request.CategoryId;
        current.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<Book>.Ok(current, "Libro actualizado"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/books/{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var current = await db.Books.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound();
        db.Books.Remove(current);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
