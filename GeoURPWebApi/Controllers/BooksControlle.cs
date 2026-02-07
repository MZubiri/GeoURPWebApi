using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class BooksController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/books")]
    public ActionResult<ApiResponse<IEnumerable<Book>>> GetPublic([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<Book>>.Ok(store.Books.Where(x => x.IsActive).OrderByDescending(x => x.Year).ToList()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/books")]
    public ActionResult<ApiResponse<IEnumerable<Book>>> GetAdmin([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<Book>>.Ok(store.Books.OrderByDescending(x => x.Year).ToList()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/books")]
    public ActionResult<ApiResponse<Book>> Create([FromBody] Book request, [FromServices] InMemoryStore store)
    {
        request.Id = NextId(store.Books.Select(x => x.Id)); store.Books.Add(request);
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<Book>.Ok(request, "Libro creado"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/books/{id:int}")]
    public ActionResult<ApiResponse<Book>> Update(int id, [FromBody] Book request, [FromServices] InMemoryStore store)
    {
        var current = store.Books.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Book>.Fail("No existe el libro"));
        current.Title = request.Title; current.Author = request.Author; current.Editorial = request.Editorial; current.Year = request.Year; current.FileUrl = request.FileUrl; current.CategoryId = request.CategoryId; current.IsActive = request.IsActive;
        return Ok(ApiResponse<Book>.Ok(current, "Libro actualizado"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/books/{id:int}")]
    public ActionResult Delete(int id, [FromServices] InMemoryStore store)
    {
        var current = store.Books.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound();
        store.Books.Remove(current); return NoContent();
    }

    private static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}