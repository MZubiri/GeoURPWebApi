namespace GeoURPWebApi.DTOs
{
    public sealed class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public IEnumerable<string> Errors { get; set; } = [];

        public static ApiResponse<T> Ok(T data, string message = "Operación exitosa") =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message, params string[] errors) =>
            new() { Success = false, Message = message, Errors = errors };
    }
}
