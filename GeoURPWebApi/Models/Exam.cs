namespace GeoURPWebApi.Models
{
    public sealed class Exam
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
