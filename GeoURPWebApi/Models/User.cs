namespace GeoURPWebApi.Models
{
    public sealed class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public List<string> Roles { get; set; } = [];
    }

}
