using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class EventsController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/events")]
    public ActionResult<ApiResponse<IEnumerable<Event>>> GetPublic([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<Event>>.Ok(store.Events.Where(x => x.IsPublic).OrderBy(x => x.StartAt).ToList()));

    [AllowAnonymous]
    [HttpGet("public/events/calendar")]
    public ActionResult<ApiResponse<IEnumerable<CalendarItemResponse>>> Calendar([FromQuery] int month, [FromQuery] int year, [FromServices] InMemoryStore store)
    {
        if (month is < 1 or > 12 || year < 2000) return BadRequest(ApiResponse<IEnumerable<CalendarItemResponse>>.Fail("Parámetros inválidos"));
        var data = store.Events.Where(x => x.IsPublic && x.StartAt.Month == month && x.StartAt.Year == year).OrderBy(x => x.StartAt).Select(x => new CalendarItemResponse { Id = x.Id, Title = x.Title, StartAt = x.StartAt, EndAt = x.EndAt, Location = x.Location }).ToList();
        return Ok(ApiResponse<IEnumerable<CalendarItemResponse>>.Ok(data));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/events")]
    public ActionResult<ApiResponse<IEnumerable<Event>>> GetAdmin([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<Event>>.Ok(store.Events.OrderBy(x => x.StartAt).ToList()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/events")]
    public ActionResult<ApiResponse<Event>> Create([FromBody] Event request, [FromServices] InMemoryStore store)
    {
        request.Id = NextId(store.Events.Select(x => x.Id)); store.Events.Add(request);
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<Event>.Ok(request, "Evento creado"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/events/{id:int}")]
    public ActionResult<ApiResponse<Event>> Update(int id, [FromBody] Event request, [FromServices] InMemoryStore store)
    {
        var current = store.Events.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Event>.Fail("No existe el evento"));
        current.Title = request.Title; current.Description = request.Description; current.StartAt = request.StartAt; current.EndAt = request.EndAt; current.Location = request.Location; current.IsPublic = request.IsPublic;
        return Ok(ApiResponse<Event>.Ok(current, "Evento actualizado"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/events/{id:int}")]
    public ActionResult Delete(int id, [FromServices] InMemoryStore store)
    {
        var current = store.Events.FirstOrDefault(x => x.Id == id);
        if (current is null) return NotFound();
        store.Events.Remove(current); return NoContent();
    }

    private static int NextId(IEnumerable<int> ids) => ids.Any() ? ids.Max() + 1 : 1;
}