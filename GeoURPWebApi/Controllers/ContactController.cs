using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using GeoURPWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class ContactController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("public/contact")]
    public ActionResult<ApiResponse<ContactMessage>> Create([FromBody] ContactMessage request, [FromServices] InMemoryStore store)
    {
        request.Id = store.ContactMessages.Any() ? store.ContactMessages.Max(x => x.Id) + 1 : 1;
        request.CreatedAt = DateTime.UtcNow;
        store.ContactMessages.Add(request);
        return Created(string.Empty, ApiResponse<ContactMessage>.Ok(request, "Mensaje recibido"));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin/contact")]
    public ActionResult<ApiResponse<IEnumerable<ContactMessage>>> GetAll([FromServices] InMemoryStore store)
        => Ok(ApiResponse<IEnumerable<ContactMessage>>.Ok(store.ContactMessages.OrderByDescending(x => x.CreatedAt).ToList()));
}