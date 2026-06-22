using System.ComponentModel.DataAnnotations.Schema;

namespace GeoURPWebApi.Models
{
    public sealed class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public string? Phone { get; set; }
        public string? Major { get; set; }
        public string? Cycle { get; set; }
        public string? Position { get; set; }
        public string? Code { get; set; }
        public string? Birthday { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Bio { get; set; }
        public int? SortOrder { get; set; }
        public string? LinkedInUrl { get; set; }

        [NotMapped]
        public List<string> Roles { get; set; } = [];

        public List<UserRole> UserRoles { get; set; } = [];
    }
}
