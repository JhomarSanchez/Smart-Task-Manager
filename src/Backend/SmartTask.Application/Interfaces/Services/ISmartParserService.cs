using System.Threading;
using System.Threading.Tasks;
using SmartTask.Application.DTOs;

namespace SmartTask.Application.Interfaces.Services;

public interface ISmartParserService
{
    Task<SmartParseResponseDto> ParseAsync(string text, CancellationToken cancellationToken);
}
