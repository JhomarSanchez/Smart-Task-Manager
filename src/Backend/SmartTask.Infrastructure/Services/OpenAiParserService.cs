using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces.Services;
using SmartTask.Infrastructure.Configuration;

namespace SmartTask.Infrastructure.Services;

public class OpenAiParserService : ISmartParserService
{
    private readonly AiSettings _settings;
    private readonly ILogger<OpenAiParserService> _logger;

    public OpenAiParserService(
        AiSettings settings,
        ILogger<OpenAiParserService> logger)
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
            _logger.LogInformation("OpenAI parser is disabled or ApiKey is missing. Returning fallback.");
            return GetFallbackResponse(text);
        }

        try
        {
            var jsonResponse = await CallOpenAiApiAsync(text, cancellationToken);
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
            _logger.LogError(ex, "Failed to parse task via OpenAI service. Returning fallback.");
            return GetFallbackResponse(text);
        }
    }

    protected virtual async Task<string> CallOpenAiApiAsync(string text, CancellationToken cancellationToken)
    {
        var client = new ChatClient(model: _settings.ModelName, apiKey: _settings.ApiKey);
        var referenceTime = DateTimeOffset.UtcNow;
        var systemMessage = $"Extract task details. The current user date/time context is {referenceTime:o}. Today is {referenceTime.DayOfWeek}. Fill all properties based on the schema.";

        string schemaJson = @"
        {
          ""type"": ""object"",
          ""properties"": {
            ""title"": {
              ""type"": ""string"",
              ""description"": ""A concise title summarizing the task. Extract the key action. If no clear action, summarize the text.""
            },
            ""description"": {
              ""type"": ""string"",
              ""description"": ""Additional context, notes, dates, or lists found in the text. Do not repeat the title here. Leave empty if no extra context is found.""
            },
            ""dueDate"": {
              ""type"": [""string"", ""null""],
              ""description"": ""The ISO 8601 date-time string of the deadline or event. Translate relative times using the client's local reference time. Return null if no date is mentioned.""
            },
            ""priority"": {
              ""type"": ""string"",
              ""enum"": [""Low"", ""Medium"", ""High""],
              ""description"": ""Assign priority based on words like 'urgente', 'importante' (High), or 'cuando puedas' (Low). Default to 'Medium'.""
            },
            ""category"": {
              ""type"": ""string"",
              ""description"": ""A single word category indicating the area of the task. Default to 'General'.""
            }
          },
          ""required"": [""title"", ""description"", ""dueDate"", ""priority"", ""category""],
          ""additionalProperties"": false
        }";

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "smart_task_schema",
                jsonSchema: BinaryData.FromString(schemaJson),
                jsonSchemaFormatDescription: "Task parsing schema",
                jsonSchemaIsStrict: true
            )
        };

        var messages = new ChatMessage[]
        {
            new SystemChatMessage(systemMessage),
            new UserChatMessage(text)
        };

        ChatCompletion completion = await client.CompleteChatAsync(messages, options, cancellationToken);
        return completion.Content[0].Text ?? throw new InvalidOperationException("OpenAI returned empty text.");
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
