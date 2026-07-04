namespace GeoURPWebApi.Models
{
    public sealed class Exam
    {
        public int Id { get; set; }
        public string Ciclo { get; set; } = string.Empty;
        public string Curso { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Periodo { get; set; } = string.Empty;
        public string Docente { get; set; } = string.Empty;
        public bool Resuelto { get; set; }
        public double? Nota { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
