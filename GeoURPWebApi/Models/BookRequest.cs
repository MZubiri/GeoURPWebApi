using System.ComponentModel.DataAnnotations.Schema;

namespace GeoURPWebApi.Models
{
    public sealed class BookRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, Sent, Rejected
        public DateTime? SentAt { get; set; }
        public int? SentByUserId { get; set; }
        public string Notes { get; set; } = string.Empty;

        // Propiedades de navegación
        public User User { get; set; } = null!;
        public Book Book { get; set; } = null!;
        
        [NotMapped]
        public User? SentByUser { get; set; }
    }
}