namespace GeoURPWebApi.DTOs
{
    public sealed class CreateBookRequestDto
    {
        public int BookId { get; set; }
    }

    public sealed class BookRequestResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public string BookFileUrl { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? SentAt { get; set; }
        public string? SentByUserName { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public sealed class SendBookRequestDto
    {
        public string? Notes { get; set; }
    }
}