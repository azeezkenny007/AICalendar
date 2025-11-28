namespace AICalendar.API.Models;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string[] Errors { get; set; } = Array.Empty<string>();
}
