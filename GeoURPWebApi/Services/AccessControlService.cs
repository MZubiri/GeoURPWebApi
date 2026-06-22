using GeoURPWebApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GeoURPWebApi.Services;

public sealed class AccessControlService
{
    private static readonly HashSet<string> UsersViewEmails = new(StringComparer.OrdinalIgnoreCase)
    {
        "201521216@urp.edu.pe",
        "molinaz.dev@gmail.com"
    };

    private static readonly HashSet<string> OrdersAccessRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "Editor",
        "Administrador",
        "Administrativo"
    };

    // Autorizado para gestionar la Junta Directiva (Presidente, Vicepresidente, Admin)
    public async Task<bool> CanAccessMembersAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (principal.IsInRole("Admin")) return true;
        return await CanAccessMembersAsync(GetCurrentEmail(principal), db, cancellationToken);
    }

    public async Task<bool> CanAccessMembersAsync(
        string? email,
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail is null) return false;

        // Verificar si el usuario tiene cargo de Presidencia o Vicepresidencia
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail && x.IsActive, cancellationToken);
        if (user is null) return false;

        if (user.Position == null) return false;

        var position = user.Position.ToLowerInvariant();
        return position.Contains("presidente") || position.Contains("vicepresidente") || position.Contains("vise");
    }

    // Autorizado para visualizar la lista general de miembros (Cualquier Directivo o Admin)
    public async Task<bool> CanAccessUsersViewAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (principal.IsInRole("Admin")) return true;
        return await CanAccessUsersViewAsync(GetCurrentEmail(principal), db, cancellationToken);
    }

    public async Task<bool> CanAccessUsersViewAsync(
        string? email,
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail is null) return false;
        if (UsersViewEmails.Contains(normalizedEmail)) return true;

        var user = await db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail && x.IsActive, cancellationToken);
        if (user is null) return false;

        // Si tiene cualquier posición asignada, es directivo y puede ver la lista de miembros
        return !string.IsNullOrWhiteSpace(user.Position);
    }

    // Autorizado para aprobar postulantes (Presidente, Vicepresidente, Dir. RRHH, Admin)
    public async Task<bool> CanApproveRegistrationsAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        if (principal.IsInRole("Admin")) return true;
        return await CanApproveRegistrationsAsync(GetCurrentEmail(principal), db, cancellationToken);
    }

    public async Task<bool> CanApproveRegistrationsAsync(
        string? email,
        AppDbContext db,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);
        if (normalizedEmail is null) return false;

        var user = await db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail && x.IsActive, cancellationToken);
        if (user is null) return false;

        if (user.Position == null) return false;

        var pos = user.Position.ToLowerInvariant();
        return pos.Contains("presidente") 
            || pos.Contains("vicepresidente") 
            || pos.Contains("vise") 
            || pos.Contains("rrhh") 
            || pos.Contains("rh");
    }

    public bool CanAccessOrders(ClaimsPrincipal principal)
    {
        var roles = principal.Claims
            .Where(claim =>
                claim.Type == ClaimTypes.Role
                || claim.Type == "role"
                || claim.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return roles.Any(role => OrdersAccessRoles.Contains(role.Trim()));
    }

    public string? GetCurrentEmail(ClaimsPrincipal principal)
        => NormalizeEmail(
            principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email")
            ?? principal.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
            ?? principal.FindFirstValue("unique_name"));

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
