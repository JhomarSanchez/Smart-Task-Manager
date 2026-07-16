using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SmartTask.Application.DTOs;
using SmartTask.Application.Interfaces.Services;

namespace SmartTask.Infrastructure.Services;

public class MockSmartParserService : ISmartParserService
{
    public Task<SmartParseResponseDto> ParseAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new SmartParseResponseDto(
                IsParsed: false,
                Title: string.Empty,
                Description: null,
                DueDate: null,
                Priority: "Medium",
                Category: "General"
            ));
        }

        // Rule-based parsing
        bool isParsed = false;
        string title = text;
        string? description = null;
        DateTimeOffset? dueDate = null;
        string priority = "Medium";
        string category = "General";

        // 1. Detect priority
        if (text.Contains("urgente", StringComparison.OrdinalIgnoreCase) || 
            text.Contains("importante", StringComparison.OrdinalIgnoreCase))
        {
            priority = "High";
            isParsed = true;
        }
        else if (text.Contains("cuando puedas", StringComparison.OrdinalIgnoreCase))
        {
            priority = "Low";
            isParsed = true;
        }

        // 2. Detect category
        if (text.Contains("reunión", StringComparison.OrdinalIgnoreCase) || 
            text.Contains(" reunion", StringComparison.OrdinalIgnoreCase) || 
            text.Contains("meeting", StringComparison.OrdinalIgnoreCase))
        {
            category = "Meeting";
            isParsed = true;
        }
        else if (text.Contains("personal", StringComparison.OrdinalIgnoreCase))
        {
            category = "Personal";
            isParsed = true;
        }

        // 3. Detect due date
        if (text.Contains("mañana", StringComparison.OrdinalIgnoreCase))
        {
            isParsed = true;
            var tomorrow = DateTimeOffset.UtcNow.AddDays(1);
            int hour = 12;
            int minute = 0;

            var timeMatch = Regex.Match(text, @"a las\s+(\d{1,2})(?::(\d{2}))?", RegexOptions.IgnoreCase);
            if (timeMatch.Success)
            {
                hour = int.Parse(timeMatch.Groups[1].Value);
                if (timeMatch.Groups[2].Success)
                {
                    minute = int.Parse(timeMatch.Groups[2].Value);
                }
            }
            dueDate = new DateTimeOffset(tomorrow.Year, tomorrow.Month, tomorrow.Day, hour, minute, 0, TimeSpan.Zero);
            description = text; // Pre-fill description with full text
        }

        // 4. Extract title by stripping out keywords and trailing time expressions
        if (isParsed)
        {
            var keywords = new[] { "mañana", "urgente", "importante", "cuando puedas" };
            foreach (var keyword in keywords)
            {
                int idx = title.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    title = title.Substring(0, idx).Trim();
                }
            }

            // Remove trailing "a las ..." or similar if it is at the end
            title = Regex.Replace(title, @"\s+a las\s+\d{1,2}(?::\d{2})?.*$", "", RegexOptions.IgnoreCase).Trim();
        }
        else
        {
            // Fallback: isParsed = false, title is full text
            title = text;
        }

        return Task.FromResult(new SmartParseResponseDto(
            IsParsed: isParsed,
            Title: title,
            Description: description,
            DueDate: dueDate,
            Priority: priority,
            Category: category
        ));
    }
}
