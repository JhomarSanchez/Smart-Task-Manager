namespace SmartTask.Infrastructure.Configuration;

public class AiSettings
{
    public const string SectionName = "AiSettings";

    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "Gemini";
    public string ApiKey { get; set; } = string.Empty;
    public string ModelName { get; set; } = "gemini-1.5-flash";
}
