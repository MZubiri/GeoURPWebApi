namespace GeoURPWebApi.Models
{
    public sealed class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Editorial { get; set; } = string.Empty;
        public int Year { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
