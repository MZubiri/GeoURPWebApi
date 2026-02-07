using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class EventsController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("public/events")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Event>>>> GetPublic([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Event>>.Ok(await db.Events.Where(x => x.IsPublic).OrderBy(x => x.StartAt).ToListAsync()));

    [AllowAnonymous]
    [HttpGet("public/events/calendar")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CalendarItemResponse>>>> Calendar([FromQuery] int month, [FromQuery] int year, [FromServices] AppDbContext db)
    {
        if (month is < 1 or > 12 || year < 2000) return BadRequest(ApiResponse<IEnumerable<CalendarItemResponse>>.Fail("Parámetros inválidos"));
        var data = await db.Events
            .Where(x => x.IsPublic && x.StartAt.Month == month && x.StartAt.Year == year)
            .OrderBy(x => x.StartAt)
            .Select(x => new CalendarItemResponse { Id = x.Id, Title = x.Title, StartAt = x.StartAt, EndAt = x.EndAt, Location = x.Location })
            .ToListAsync();
        return Ok(ApiResponse<IEnumerable<CalendarItemResponse>>.Ok(data));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpGet("admin/events")]
    public async Task<ActionResult<ApiResponse<IEnumerable<Event>>>> GetAdmin([FromServices] AppDbContext db)
        => Ok(ApiResponse<IEnumerable<Event>>.Ok(await db.Events.OrderBy(x => x.StartAt).ToListAsync()));

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("admin/events")]
    public async Task<ActionResult<ApiResponse<Event>>> Create([FromBody] Event request, [FromServices] AppDbContext db)
    {
        db.Events.Add(request);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetAdmin), ApiResponse<Event>.Ok(request, "Evento creado"));
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("admin/events/{id:int}")]
    public async Task<ActionResult<ApiResponse<Event>>> Update(int id, [FromBody] Event request, [FromServices] AppDbContext db)
    {
        var current = await db.Events.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound(ApiResponse<Event>.Fail("No existe el evento"));
        current.Title = request.Title;
        current.Description = request.Description;
        current.StartAt = request.StartAt;
        current.EndAt = request.EndAt;
        current.Location = request.Location;
        current.IsPublic = request.IsPublic;
        await db.SaveChangesAsync();
        return Ok(ApiResponse<Event>.Ok(current, "Evento actualizado"));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("admin/events/{id:int}")]
    public async Task<ActionResult> Delete(int id, [FromServices] AppDbContext db)
    {
        var current = await db.Events.FirstOrDefaultAsync(x => x.Id == id);
        if (current is null) return NotFound();
        db.Events.Remove(current);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
