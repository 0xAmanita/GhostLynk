namespace backend_deob.DTOs;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Message { get; set; }
    public List<string>? Details { get; set; }
}
