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
