namespace SmartTask.Infrastructure.Services;

public class AiParsedResult
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DueDate { get; set; }
    public string Priority { get; set; } = "Medium";
    public string Category { get; set; } = "General";
}
