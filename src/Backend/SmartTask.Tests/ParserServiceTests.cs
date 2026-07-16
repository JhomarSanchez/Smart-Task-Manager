using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartTask.Application.Interfaces.Services;
using SmartTask.Infrastructure;
using SmartTask.Infrastructure.Configuration;
using SmartTask.Infrastructure.Services;
using Xunit;

namespace SmartTask.Tests;

public class ParserServiceTests
{
    private class TestableGeminiParserService : GeminiParserService
    {
        public Func<string, Task<string>>? GeminiApiMock { get; set; }

        public TestableGeminiParserService(AiSettings settings)
            : base(settings, NullLogger<GeminiParserService>.Instance)
        {
        }

        protected override async Task<string> CallGeminiApiAsync(string text, CancellationToken cancellationToken)
        {
            if (GeminiApiMock != null)
            {
                return await GeminiApiMock(text);
            }
            return await base.CallGeminiApiAsync(text, cancellationToken);
        }
    }

    private class TestableOpenAiParserService : OpenAiParserService
    {
        public Func<string, Task<string>>? OpenAiApiMock { get; set; }

        public TestableOpenAiParserService(AiSettings settings)
            : base(settings, NullLogger<OpenAiParserService>.Instance)
        {
        }

        protected override async Task<string> CallOpenAiApiAsync(string text, CancellationToken cancellationToken)
        {
            if (OpenAiApiMock != null)
            {
                return await OpenAiApiMock(text);
            }
            return await base.CallOpenAiApiAsync(text, cancellationToken);
        }
    }

    [Fact]
    public async Task GeminiParser_WhenDisabled_ShouldReturnFallback()
    {
        // Arrange
        var settings = new AiSettings { Enabled = false, Provider = "Gemini", ApiKey = "dummy-key" };
        var service = new GeminiParserService(settings, NullLogger<GeminiParserService>.Instance);
        var rawText = "Implement Phase 3 task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
        result.Description.Should().BeNull();
        result.DueDate.Should().BeNull();
        result.Priority.Should().Be("Medium");
        result.Category.Should().Be("General");
    }

    [Fact]
    public async Task GeminiParser_WhenApiKeyMissing_ShouldReturnFallback()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = "" };
        var service = new GeminiParserService(settings, NullLogger<GeminiParserService>.Instance);
        var rawText = "Implement Phase 3 task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task GeminiParser_WhenApiThrowsException_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = "dummy-key" };
        var service = new TestableGeminiParserService(settings)
        {
            GeminiApiMock = text => throw new System.Net.Http.HttpRequestException("Offline network error")
        };
        var rawText = "Implement Phase 3 task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
        result.Description.Should().BeNull();
        result.DueDate.Should().BeNull();
        result.Priority.Should().Be("Medium");
        result.Category.Should().Be("General");
    }

    [Fact]
    public async Task GeminiParser_WhenSuccessful_ShouldReturnParsedResult()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = "dummy-key" };
        var expectedDueDate = DateTimeOffset.UtcNow.AddDays(1);
        var service = new TestableGeminiParserService(settings)
        {
            GeminiApiMock = text => Task.FromResult($@"
            {{
                ""title"": ""Extracted Task"",
                ""description"": ""Task details description"",
                ""dueDate"": ""{expectedDueDate:o}"",
                ""priority"": ""High"",
                ""category"": ""Development""
            }}")
        };

        // Act
        var result = await service.ParseAsync("raw text", default);

        // Assert
        result.IsParsed.Should().BeTrue();
        result.Title.Should().Be("Extracted Task");
        result.Description.Should().Be("Task details description");
        result.DueDate.Should().NotBeNull();
        result.DueDate!.Value.Year.Should().Be(expectedDueDate.Year);
        result.DueDate!.Value.Month.Should().Be(expectedDueDate.Month);
        result.DueDate!.Value.Day.Should().Be(expectedDueDate.Day);
        result.Priority.Should().Be("High");
        result.Category.Should().Be("Development");
    }

    [Fact]
    public async Task OpenAiParser_WhenDisabled_ShouldReturnFallback()
    {
        // Arrange
        var settings = new AiSettings { Enabled = false, Provider = "OpenAI", ApiKey = "dummy-key" };
        var service = new OpenAiParserService(settings, NullLogger<OpenAiParserService>.Instance);
        var rawText = "Implement Phase 3 task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
        result.Description.Should().BeNull();
        result.DueDate.Should().BeNull();
        result.Priority.Should().Be("Medium");
        result.Category.Should().Be("General");
    }

    [Fact]
    public async Task OpenAiParser_WhenApiKeyMissing_ShouldReturnFallback()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = "" };
        var service = new OpenAiParserService(settings, NullLogger<OpenAiParserService>.Instance);
        var rawText = "Implement Phase 3 task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task OpenAiParser_WhenApiThrowsException_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = "dummy-key" };
        var service = new TestableOpenAiParserService(settings)
        {
            OpenAiApiMock = text => throw new System.Net.Http.HttpRequestException("Offline network error")
        };
        var rawText = "Implement Phase 3 task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
        result.Description.Should().BeNull();
        result.DueDate.Should().BeNull();
        result.Priority.Should().Be("Medium");
        result.Category.Should().Be("General");
    }

    [Fact]
    public async Task OpenAiParser_WhenSuccessful_ShouldReturnParsedResult()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = "dummy-key" };
        var expectedDueDate = DateTimeOffset.UtcNow.AddDays(2);
        var service = new TestableOpenAiParserService(settings)
        {
            OpenAiApiMock = text => Task.FromResult($@"
            {{
                ""title"": ""OpenAI Extracted Task"",
                ""description"": ""OpenAI details description"",
                ""dueDate"": ""{expectedDueDate:o}"",
                ""priority"": ""Low"",
                ""category"": ""Personal""
            }}")
        };

        // Act
        var result = await service.ParseAsync("raw text", default);

        // Assert
        result.IsParsed.Should().BeTrue();
        result.Title.Should().Be("OpenAI Extracted Task");
        result.Description.Should().Be("OpenAI details description");
        result.DueDate.Should().NotBeNull();
        result.DueDate!.Value.Year.Should().Be(expectedDueDate.Year);
        result.DueDate!.Value.Month.Should().Be(expectedDueDate.Month);
        result.DueDate!.Value.Day.Should().Be(expectedDueDate.Day);
        result.Priority.Should().Be("Low");
        result.Category.Should().Be("Personal");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GeminiParser_WhenTextNullOrEmpty_ShouldReturnEmptyFallback(string? text)
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = "dummy-key" };
        var service = new GeminiParserService(settings, Microsoft.Extensions.Logging.Abstractions.NullLogger<GeminiParserService>.Instance);

        // Act
        var result = await service.ParseAsync(text!, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().BeEmpty();
        result.Description.Should().BeNull();
        result.DueDate.Should().BeNull();
        result.Priority.Should().Be("Medium");
        result.Category.Should().Be("General");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OpenAiParser_WhenTextNullOrEmpty_ShouldReturnEmptyFallback(string? text)
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = "dummy-key" };
        var service = new OpenAiParserService(settings, Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenAiParserService>.Instance);

        // Act
        var result = await service.ParseAsync(text!, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().BeEmpty();
        result.Description.Should().BeNull();
        result.DueDate.Should().BeNull();
        result.Priority.Should().Be("Medium");
        result.Category.Should().Be("General");
    }

    [Fact]
    public void DependencyInjection_WhenUnsupportedProvider_ShouldFallbackToGemini()
    {
        // Arrange
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string?>
            {
                { "AiSettings:Enabled", "true" },
                { "AiSettings:Provider", "UnsupportedProvider" },
                { "AiSettings:ApiKey", "some-key" }
            })
            .Build();

        // Act
        SmartTask.Infrastructure.DependencyInjection.AddInfrastructureServices(services, configuration);
        services.AddLogging(); // Need this because GeminiParserService needs ILogger
        var provider = services.BuildServiceProvider();
        var parserService = provider.GetService<SmartTask.Application.Interfaces.Services.ISmartParserService>();

        // Assert
        parserService.Should().NotBeNull();
        parserService.Should().BeOfType<DynamicSmartParserService>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task GeminiParser_WhenApiKeyWhitespaceOrNull_ShouldReturnFallback(string? apiKey)
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = apiKey! };
        var service = new GeminiParserService(settings, Microsoft.Extensions.Logging.Abstractions.NullLogger<GeminiParserService>.Instance);
        var rawText = "Implement Phase 3 task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task OpenAiParser_WhenApiKeyWhitespaceOrNull_ShouldReturnFallback(string? apiKey)
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = apiKey! };
        var service = new OpenAiParserService(settings, Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenAiParserService>.Instance);
        var rawText = "Implement Phase 3 task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task GeminiParser_WhenApiThrowsTimeoutException_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = "dummy-key" };
        var service = new TestableGeminiParserService(settings)
        {
            GeminiApiMock = text => throw new TimeoutException("API call timed out")
        };
        var rawText = "Timeout task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task OpenAiParser_WhenApiThrowsTimeoutException_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = "dummy-key" };
        var service = new TestableOpenAiParserService(settings)
        {
            OpenAiApiMock = text => throw new TimeoutException("API call timed out")
        };
        var rawText = "Timeout task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task GeminiParser_WhenApiReturnsMalformedJson_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = "dummy-key" };
        var service = new TestableGeminiParserService(settings)
        {
            GeminiApiMock = text => Task.FromResult("this is not json")
        };
        var rawText = "Malformed JSON task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task OpenAiParser_WhenApiReturnsMalformedJson_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = "dummy-key" };
        var service = new TestableOpenAiParserService(settings)
        {
            OpenAiApiMock = text => Task.FromResult("this is not json")
        };
        var rawText = "Malformed JSON task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task GeminiParser_WhenApiReturnsJsonMissingTitle_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = "dummy-key" };
        var service = new TestableGeminiParserService(settings)
        {
            GeminiApiMock = text => Task.FromResult("{\"description\": \"missing title\"}")
        };
        var rawText = "Missing title task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task OpenAiParser_WhenApiReturnsJsonMissingTitle_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = "dummy-key" };
        var service = new TestableOpenAiParserService(settings)
        {
            OpenAiApiMock = text => Task.FromResult("{\"description\": \"missing title\"}")
        };
        var rawText = "Missing title task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task GeminiParser_WhenApiReturnsUnexpectedJsonTypes_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = "dummy-key" };
        var service = new TestableGeminiParserService(settings)
        {
            GeminiApiMock = text => Task.FromResult("{\"title\": 12345, \"description\": true}")
        };
        var rawText = "Unexpected types task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task OpenAiParser_WhenApiReturnsUnexpectedJsonTypes_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = "dummy-key" };
        var service = new TestableOpenAiParserService(settings)
        {
            OpenAiApiMock = text => Task.FromResult("{\"title\": 12345, \"description\": true}")
        };
        var rawText = "Unexpected types task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task GeminiParser_WhenApiReturnsInvalidPriorityOrCategory_ShouldKeepValuesInDto()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = "dummy-key" };
        var service = new TestableGeminiParserService(settings)
        {
            GeminiApiMock = text => Task.FromResult("{\"title\": \"Valid Title\", \"priority\": \"Critical\", \"category\": null}")
        };

        // Act
        var result = await service.ParseAsync("raw text", default);

        // Assert
        result.IsParsed.Should().BeTrue();
        result.Title.Should().Be("Valid Title");
        result.Priority.Should().Be("Critical");
        result.Category.Should().BeNull();
    }

    [Fact]
    public async Task OpenAiParser_WhenApiReturnsInvalidPriorityOrCategory_ShouldKeepValuesInDto()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = "dummy-key" };
        var service = new TestableOpenAiParserService(settings)
        {
            OpenAiApiMock = text => Task.FromResult("{\"title\": \"Valid Title\", \"priority\": \"Critical\", \"category\": null}")
        };

        // Act
        var result = await service.ParseAsync("raw text", default);

        // Assert
        result.IsParsed.Should().BeTrue();
        result.Title.Should().Be("Valid Title");
        result.Priority.Should().Be("Critical");
        result.Category.Should().BeNull();
    }

    [Fact]
    public async Task GeminiParser_WhenCancellationTokenCancelled_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "Gemini", ApiKey = "dummy-key" };
        var service = new TestableGeminiParserService(settings)
        {
            GeminiApiMock = text => throw new OperationCanceledException()
        };
        var rawText = "Cancelled task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }

    [Fact]
    public async Task OpenAiParser_WhenCancellationTokenCancelled_ShouldReturnFallbackGracefully()
    {
        // Arrange
        var settings = new AiSettings { Enabled = true, Provider = "OpenAI", ApiKey = "dummy-key" };
        var service = new TestableOpenAiParserService(settings)
        {
            OpenAiApiMock = text => throw new OperationCanceledException()
        };
        var rawText = "Cancelled task";

        // Act
        var result = await service.ParseAsync(rawText, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(rawText);
    }
}


