namespace GeoURPWebApi.Models
{
    public sealed class Research
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public DateTime PublishedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
