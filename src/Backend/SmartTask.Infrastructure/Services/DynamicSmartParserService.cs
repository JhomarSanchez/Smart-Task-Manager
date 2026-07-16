using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces.Services;
using SmartTask.Infrastructure.Configuration;

namespace SmartTask.Infrastructure.Services;

public class DynamicSmartParserService : ISmartParserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AiSettings _defaultSettings;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DynamicSmartParserService> _logger;

    public DynamicSmartParserService(
        IHttpContextAccessor httpContextAccessor,
        AiSettings defaultSettings,
        ILoggerFactory loggerFactory)
    {
        _httpContextAccessor = httpContextAccessor;
        _defaultSettings = defaultSettings;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DynamicSmartParserService>();
    }

    public async Task<SmartParseResponseDto> ParseAsync(string text, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        string? provider = null;
        string? apiKey = null;
        string? modelName = null;
        bool enabled = true;

        if (httpContext != null)
        {
            if (httpContext.Request.Headers.TryGetValue("X-AI-ApiKey", out var apiKeyValues))
            {
                apiKey = apiKeyValues.ToString();
            }

            if (httpContext.Request.Headers.TryGetValue("X-AI-Provider", out var providerValues))
            {
                provider = providerValues.ToString();
            }

            if (httpContext.Request.Headers.TryGetValue("X-AI-ModelName", out var modelValues))
            {
                modelName = modelValues.ToString();
            }

            if (httpContext.Request.Headers.TryGetValue("X-AI-Enabled", out var enabledValues))
            {
                _ = bool.TryParse(enabledValues.ToString(), out enabled);
            }
        }

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            provider = string.IsNullOrWhiteSpace(provider) ? "Gemini" : provider;
            modelName = string.IsNullOrWhiteSpace(modelName) 
                ? (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) ? "gpt-4o-mini" : "gemini-1.5-flash") 
                : modelName;

            _logger.LogInformation("Using dynamic request-level AI settings with provider: {Provider}", provider);

            var dynamicSettings = new AiSettings
            {
                Enabled = enabled,
                Provider = provider,
                ApiKey = apiKey,
                ModelName = modelName
            };

            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                var parser = new OpenAiParserService(dynamicSettings, _loggerFactory.CreateLogger<OpenAiParserService>());
                return await parser.ParseAsync(text, cancellationToken);
            }
            else
            {
                var parser = new GeminiParserService(dynamicSettings, _loggerFactory.CreateLogger<GeminiParserService>());
                return await parser.ParseAsync(text, cancellationToken);
            }
        }

        // Fallback to default server settings
        _logger.LogInformation("Using default server-configured AI parser service.");
        if (string.Equals(_defaultSettings.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            var parser = new OpenAiParserService(_defaultSettings, _loggerFactory.CreateLogger<OpenAiParserService>());
            return await parser.ParseAsync(text, cancellationToken);
        }
        else
        {
            var parser = new GeminiParserService(_defaultSettings, _loggerFactory.CreateLogger<GeminiParserService>());
            return await parser.ParseAsync(text, cancellationToken);
        }
    }
}
