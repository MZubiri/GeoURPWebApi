using GeoURPWebApi.Data;
using GeoURPWebApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Pdf.IO;
using System.IO;
using System.Security.Claims;

namespace GeoURPWebApi.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class LibraryDownloadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public LibraryDownloadController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [Authorize]
    [HttpGet("library/download")]
    public async Task<IActionResult> DownloadFile([FromQuery] string fileUrl, [FromServices] AppDbContext db)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return BadRequest(ApiResponse<string>.Fail("La URL del archivo es requerida."));
            }

            // Extract user email and identity information from claims
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value 
                            ?? User.FindFirst("email")?.Value;

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? User.FindFirst("sub")?.Value;

            var userName = User.FindFirst(ClaimTypes.Name)?.Value 
                           ?? User.FindFirst("name")?.Value;

            // Fallback: search database if email isn't in claims
            if (string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                var user = await db.Users.FindAsync(userId);
                if (user != null)
                {
                    userEmail = user.Email;
                    userName = user.Name;
                }
            }

            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "anonymous@geourp.org";
            }

            // Check if it's an external file (e.g. starts with http and does not contain /uploads/)
            if (fileUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) && !fileUrl.Contains("/uploads/"))
            {
                // For external files, we cannot modify them locally, so we perform a redirect
                return Redirect(fileUrl);
            }

            // Resolve local path securely
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var localPath = ResolveLocalPath(fileUrl, webRootPath);

            if (localPath == null || !System.IO.File.Exists(localPath))
            {
                return NotFound(ApiResponse<string>.Fail("El archivo solicitado no existe o no está disponible en este servidor."));
            }

            var extension = Path.GetExtension(localPath).ToLowerInvariant();
            var contentType = GetContentType(extension);

            // Only trace PDF documents
            if (extension == ".pdf")
            {
                try
                {
                    // Open and write metadata using PdfSharpCore in memory
                    byte[] fileBytes;
                    using (var doc = PdfReader.Open(localPath, PdfDocumentOpenMode.Modify))
                    {
                        var metadata = $"DownloadedBy:{userEmail};Timestamp:{DateTime.UtcNow:o};User:{userName}";
                        doc.Info.Keywords = metadata;
                        
                        using (var ms = new MemoryStream())
                        {
                            doc.Save(ms, false);
                            fileBytes = ms.ToArray();
                        }
                    }

                    return File(fileBytes, contentType, Path.GetFileName(localPath));
                }
                catch (Exception ex)
                {
                    // Fallback to sending original file if PDF manipulation fails
                    Console.WriteLine($"Warning: Failed to inject metadata into PDF {localPath}. Error: {ex.Message}. Serving original file.");
                    return PhysicalFile(localPath, contentType, Path.GetFileName(localPath));
                }
            }

            // Return non-PDF files as-is
            return PhysicalFile(localPath, contentType, Path.GetFileName(localPath));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<string>.Fail($"Error interno al procesar la descarga: {ex.Message}"));
        }
    }

    private string? ResolveLocalPath(string fileUrl, string webRootPath)
    {
        string relativePath = fileUrl;
        
        // Handle full URLs pointing to our domain
        if (fileUrl.Contains("/uploads/"))
        {
            var idx = fileUrl.IndexOf("/uploads/");
            relativePath = fileUrl.Substring(idx + 1); // e.g. "uploads/library/research/file.pdf"
        }
        
        // Clean URL segment formatting and convert to platform path style
        relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        
        // Combine and resolve canonical absolute path
        var fullPath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
        var uploadsFolder = Path.GetFullPath(Path.Combine(webRootPath, "uploads"));

        // Safeguard: Ensure path is strictly inside wwwroot/uploads to block directory traversal
        if (!fullPath.StartsWith(uploadsFolder, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return fullPath;
    }

    private string GetContentType(string extension)
    {
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}
