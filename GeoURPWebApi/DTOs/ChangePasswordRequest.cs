namespace GeoURPWebApi.DTOs
{
    public sealed class ChangePasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}