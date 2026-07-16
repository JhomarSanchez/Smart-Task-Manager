using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Google.GenAI;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces.Services;
using SmartTask.Infrastructure.Configuration;

namespace SmartTask.Infrastructure.Services;

public class GeminiParserService : ISmartParserService
{
    private readonly AiSettings _settings;
    private readonly ILogger<GeminiParserService> _logger;

    public GeminiParserService(
        AiSettings settings,
        ILogger<GeminiParserService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<SmartParseResponseDto> ParseAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return GetFallbackResponse(string.Empty);
        }

        if (!_settings.Enabled || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogInformation("Gemini AI parser is disabled or ApiKey is missing. Returning fallback.");
            return GetFallbackResponse(text);
        }

        try
        {
            var jsonResponse = await CallGeminiApiAsync(text, cancellationToken);
            var parsedResult = JsonSerializer.Deserialize<AiParsedResult>(jsonResponse, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (parsedResult == null || string.IsNullOrWhiteSpace(parsedResult.Title))
            {
                throw new InvalidOperationException("Failed to deserialize task schema or title is empty.");
            }

            DateTimeOffset? parsedDueDate = null;
            if (!string.IsNullOrWhiteSpace(parsedResult.DueDate) && DateTimeOffset.TryParse(parsedResult.DueDate, out var date))
            {
                parsedDueDate = date;
            }

            return new SmartParseResponseDto(
                IsParsed: true,
                Title: parsedResult.Title,
                Description: parsedResult.Description,
                DueDate: parsedDueDate,
                Priority: parsedResult.Priority,
                Category: parsedResult.Category
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse task via Gemini service. Returning fallback.");
            return GetFallbackResponse(text);
        }
    }

    protected virtual async Task<string> CallGeminiApiAsync(string text, CancellationToken cancellationToken)
    {
        using var client = new Google.GenAI.Client(apiKey: _settings.ApiKey);
        var chatClient = client.AsIChatClient(_settings.ModelName);
        var referenceTime = DateTimeOffset.UtcNow;
        var prompt = $"Extract task details from: '{text}'. The current user date/time context is {referenceTime:o}. Today is {referenceTime.DayOfWeek}. Fill all properties based on the schema.";

        var options = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.ForJsonSchema<AiParsedResult>()
        };

        var response = await chatClient.GetResponseAsync(prompt, options, cancellationToken);
        return response.Text ?? throw new InvalidOperationException("Gemini returned empty text.");
    }

    private SmartParseResponseDto GetFallbackResponse(string text)
    {
        return new SmartParseResponseDto(
            IsParsed: false,
            Title: text,
            Description: null,
            DueDate: null,
            Priority: "Medium",
            Category: "General"
        );
    }
}
