using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1/admin/book-categories")]
[Authorize(Roles = "Admin,Editor")]
public sealed class BookCategoriesController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<IEnumerable<BookCategory>>> GetAll([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<BookCategory>>.Ok(store.BookCategories.OrderBy(x => x.Id).ToList()));
    [HttpPost]
    public ActionResult<ApiResponse<BookCategory>> Create([FromBody] BookCategory request, [FromServices] InMemoryStore store)
    { request.Id = NextId(store.BookCategories.Select(x => x.Id)); store.BookCategories.Add(request); return CreatedAtAction(nameof(GetAll), ApiResponse<BookCategory>.Ok(request, "Categoría creada")); }
    [HttpPut("{id:int}")]
    public ActionResult<ApiResponse<BookCategory>> Update(int id, [FromBody] BookCategory request, [FromServices] InMemoryStore store)
    { var c = store.BookCategories.FirstOrDefault(x => x.Id == id); if (c is null) return NotFound(ApiResponse<BookCategory>.Fail("No existe la categoría")); c.Name = request.Name; c.IsActive = request.IsActive; return Ok(ApiResponse<BookCategory>.Ok(c, "Categoría actualizada")); }
    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id, [FromServices] InMemoryStore store)
    { var c = store.BookCategories.FirstOrDefault(x => x.Id == id); if (c is null) return NotFound(); store.BookCategories.Remove(c); return NoContent(); }
    private static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}