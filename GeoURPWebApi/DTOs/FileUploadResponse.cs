namespace GeoURPWebApi.DTOs
{
    public sealed class FileUploadResponse
    {
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }
}