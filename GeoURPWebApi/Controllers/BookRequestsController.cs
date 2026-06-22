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
[Route("api/v1")]
public sealed class BookRequestsController : ControllerBase
{
    // Usuario autenticado solicita un libro
    [Authorize]
    [HttpPost("book-requests")]
    public async Task<ActionResult<ApiResponse<BookRequestResponseDto>>> CreateRequest(
        [FromBody] CreateBookRequestDto request,
        [FromServices] AppDbContext db)
    {
        // Obtener el ID del usuario autenticado
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            return Unauthorized(ApiResponse<BookRequestResponseDto>.Fail("Usuario no autenticado"));
        }

        var userId = int.Parse(userIdClaim);

        // Verificar que el libro exista
        var book = await db.Books.FindAsync(request.BookId);
        if (book == null)
        {
            return NotFound(ApiResponse<BookRequestResponseDto>.Fail("Libro no encontrado"));
        }

        // Verificar si ya existe una solicitud pendiente para este usuario y libro
        var existingRequest = await db.BookRequests
            .FirstOrDefaultAsync(br => br.UserId == userId && br.BookId == request.BookId && br.Status == "Pending");

        if (existingRequest != null)
        {
            return BadRequest(ApiResponse<BookRequestResponseDto>.Fail("Ya tienes una solicitud pendiente para este libro"));
        }

        // Crear la solicitud
        var bookRequest = new BookRequest
        {
            UserId = userId,
            BookId = request.BookId,
            RequestedAt = DateTime.UtcNow,
            Status = "Pending"
        };

        db.BookRequests.Add(bookRequest);
        await db.SaveChangesAsync();

        // Cargar los datos relacionados para la respuesta
        var user = await db.Users.FindAsync(userId);
        var response = new BookRequestResponseDto
        {
            Id = bookRequest.Id,
            UserId = userId,
            UserName = user!.Name,
            UserEmail = user.Email,
            BookId = book.Id,
            BookTitle = book.Title,
            BookAuthor = book.Author,
            BookFileUrl = book.FileUrl,
            RequestedAt = bookRequest.RequestedAt,
            Status = bookRequest.Status
        };

        return Ok(ApiResponse<BookRequestResponseDto>.Ok(response, "Solicitud creada exitosamente"));
    }

    // Usuario ve sus propias solicitudes
    [Authorize]
    [HttpGet("my-book-requests")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookRequestResponseDto>>>> GetMyRequests(
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!accessControlService.CanAccessOrders(User))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<IEnumerable<BookRequestResponseDto>>.Fail("No tienes permisos para ver pedidos."));
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            return Unauthorized(ApiResponse<IEnumerable<BookRequestResponseDto>>.Fail("Usuario no autenticado"));
        }

        var userId = int.Parse(userIdClaim);

        var requests = await db.BookRequests
            .Include(br => br.User)
            .Include(br => br.Book)
            .Where(br => br.UserId == userId)
            .OrderByDescending(br => br.RequestedAt)
            .Select(br => new BookRequestResponseDto
            {
                Id = br.Id,
                UserId = br.UserId,
                UserName = br.User.Name,
                UserEmail = br.User.Email,
                BookId = br.BookId,
                BookTitle = br.Book.Title,
                BookAuthor = br.Book.Author,
                BookFileUrl = br.Book.FileUrl,
                RequestedAt = br.RequestedAt,
                Status = br.Status,
                SentAt = br.SentAt,
                Notes = br.Notes
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<BookRequestResponseDto>>.Ok(requests));
    }

    // Admin/Editor ve todas las solicitudes pendientes
    [Authorize]
    [HttpGet("admin/book-requests")]
    public async Task<ActionResult<ApiResponse<IEnumerable<BookRequestResponseDto>>>> GetAllRequests(
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService,
        [FromQuery] string? status = null)
    {
        if (!accessControlService.CanAccessOrders(User))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<IEnumerable<BookRequestResponseDto>>.Fail("No tienes permisos para ver pedidos."));
        }

        var query = db.BookRequests
            .Include(br => br.User)
            .Include(br => br.Book)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(br => br.Status == status);
        }

        var requests = await query
            .OrderByDescending(br => br.RequestedAt)
            .Select(br => new BookRequestResponseDto
            {
                Id = br.Id,
                UserId = br.UserId,
                UserName = br.User.Name,
                UserEmail = br.User.Email,
                BookId = br.BookId,
                BookTitle = br.Book.Title,
                BookAuthor = br.Book.Author,
                BookFileUrl = br.Book.FileUrl,
                RequestedAt = br.RequestedAt,
                Status = br.Status,
                SentAt = br.SentAt,
                Notes = br.Notes
            })
            .ToListAsync();

        return Ok(ApiResponse<IEnumerable<BookRequestResponseDto>>.Ok(requests));
    }

    // Admin/Editor envía el libro por correo
    [Authorize]
    [HttpPost("admin/book-requests/{id:int}/send")]
    public async Task<ActionResult<ApiResponse<BookRequestResponseDto>>> SendBook(
        int id,
        [FromBody] SendBookRequestDto request,
        [FromServices] AppDbContext db,
        [FromServices] EmailService emailService,
        [FromServices] AccessControlService accessControlService)
    {
        if (!accessControlService.CanAccessOrders(User))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<BookRequestResponseDto>.Fail("No tienes permisos para gestionar pedidos."));
        }

        // Obtener el ID del admin/editor que envía
        var senderIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (senderIdClaim == null)
        {
            return Unauthorized(ApiResponse<BookRequestResponseDto>.Fail("Usuario no autenticado"));
        }

        var senderId = int.Parse(senderIdClaim);

        // Obtener la solicitud
        var bookRequest = await db.BookRequests
            .Include(br => br.User)
            .Include(br => br.Book)
            .FirstOrDefaultAsync(br => br.Id == id);

        if (bookRequest == null)
        {
            return NotFound(ApiResponse<BookRequestResponseDto>.Fail("Solicitud no encontrada"));
        }

        if (bookRequest.Status != "Pending")
        {
            return BadRequest(ApiResponse<BookRequestResponseDto>.Fail("Esta solicitud ya fue procesada"));
        }

        // Enviar el email
        var emailSent = await emailService.SendBookLinkAsync(
            bookRequest.User.Email,
            bookRequest.User.Name,
            bookRequest.Book.Title,
            bookRequest.Book.FileUrl
        );

        if (!emailSent)
        {
            return StatusCode(500, ApiResponse<BookRequestResponseDto>.Fail("Error al enviar el correo"));
        }

        // Actualizar la solicitud
        bookRequest.Status = "Sent";
        bookRequest.SentAt = DateTime.UtcNow;
        bookRequest.SentByUserId = senderId;
        bookRequest.Notes = request.Notes ?? string.Empty;

        await db.SaveChangesAsync();

        var response = new BookRequestResponseDto
        {
            Id = bookRequest.Id,
            UserId = bookRequest.UserId,
            UserName = bookRequest.User.Name,
            UserEmail = bookRequest.User.Email,
            BookId = bookRequest.BookId,
            BookTitle = bookRequest.Book.Title,
            BookAuthor = bookRequest.Book.Author,
            BookFileUrl = bookRequest.Book.FileUrl,
            RequestedAt = bookRequest.RequestedAt,
            Status = bookRequest.Status,
            SentAt = bookRequest.SentAt,
            Notes = bookRequest.Notes
        };

        return Ok(ApiResponse<BookRequestResponseDto>.Ok(response, "Libro enviado exitosamente"));
    }

    // Admin/Editor rechaza una solicitud
    [Authorize]
    [HttpPost("admin/book-requests/{id:int}/reject")]
    public async Task<ActionResult<ApiResponse<BookRequestResponseDto>>> RejectRequest(
        int id,
        [FromBody] SendBookRequestDto request,
        [FromServices] AppDbContext db,
        [FromServices] AccessControlService accessControlService)
    {
        if (!accessControlService.CanAccessOrders(User))
        {
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<BookRequestResponseDto>.Fail("No tienes permisos para gestionar pedidos."));
        }

        var bookRequest = await db.BookRequests
            .Include(br => br.User)
            .Include(br => br.Book)
            .FirstOrDefaultAsync(br => br.Id == id);

        if (bookRequest == null)
        {
            return NotFound(ApiResponse<BookRequestResponseDto>.Fail("Solicitud no encontrada"));
        }

        if (bookRequest.Status != "Pending")
        {
            return BadRequest(ApiResponse<BookRequestResponseDto>.Fail("Esta solicitud ya fue procesada"));
        }

        bookRequest.Status = "Rejected";
        bookRequest.Notes = request.Notes ?? string.Empty;
        await db.SaveChangesAsync();

        var response = new BookRequestResponseDto
        {
            Id = bookRequest.Id,
            UserId = bookRequest.UserId,
            UserName = bookRequest.User.Name,
            UserEmail = bookRequest.User.Email,
            BookId = bookRequest.BookId,
            BookTitle = bookRequest.Book.Title,
            BookAuthor = bookRequest.Book.Author,
            BookFileUrl = bookRequest.Book.FileUrl,
            RequestedAt = bookRequest.RequestedAt,
            Status = bookRequest.Status,
            Notes = bookRequest.Notes
        };

        return Ok(ApiResponse<BookRequestResponseDto>.Ok(response, "Solicitud rechazada"));
    }

    // TEMPORAL: Endpoint de prueba para email
    [AllowAnonymous]
    [HttpPost("test-email")]
    public async Task<ActionResult<ApiResponse<string>>> TestEmail(
        [FromServices] EmailService emailService,
        [FromQuery] string? email = null) // Permitir enviar a cualquier email
    {
        // Si no se proporciona email, usar el tuyo por defecto
        var testEmail = email ?? "molinaz.dev@gmail.com";

        var success = await emailService.SendBookLinkAsync(
            testEmail,
            "Miguel Zubiri",
            "Introducción a la Geología - Edición 2024",
            "https://geourp.org/libros/ejemplo-geologia.pdf"
        );

        if (success)
        {
            return Ok(ApiResponse<string>.Ok(
                $"✅ Email enviado correctamente a {testEmail}. Revisa tu bandeja de entrada (y spam).",
                "Email enviado exitosamente"
            ));
        }
        else
        {
            return StatusCode(500, ApiResponse<string>.Fail(
                "❌ Error al enviar el email. Revisa los logs de la aplicación para más detalles."
            ));
        }
    }
}
