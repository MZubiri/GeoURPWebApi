using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/book-categories")]
[Authorize(Roles = "Admin,Editor")]
public sealed class BookCategoriesController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookCategory>>>> GetAll([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<BookCategory>>.Ok(await db.BookCategories.OrderBy(x => x.Id).ToListAsync()));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BookCategory>>> Create([FromBody] BookCategory request, [FromServices] AppDbContext db)
    {
        db.BookCategories.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAll), ApiResponse<BookCategory>.Ok(request, "Categoría creada"));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<BookCategory>>> Update(int id, [FromBody] BookCategory request, [FromServices] AppDbContext db)
    {
        var c = await db.BookCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound(ApiResponse<BookCategory>.Fail("No existe la categoría"));
        c.Name = request.Name;
        c.IsActive = request.IsActive;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<BookCategory>.Ok(c, "Categoría actualizada"));
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var c = await db.BookCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        db.BookCategories.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
