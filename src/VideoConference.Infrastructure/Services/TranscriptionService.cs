using Microsoft.Extensions.Logging;
using VideoConference.Application.Interfaces;

namespace VideoConference.Infrastructure.Services;

public class TranscriptionService : ITranscriptionService
{
    private readonly ILogger<TranscriptionService> _logger;

    public TranscriptionService(ILogger<TranscriptionService> logger)
    {
        _logger = logger;
    }

    public bool IsServerTranscriptionAvailable()
    {
        // For the MVP, client-side browser speech recognition (Web Speech API) handles real-time live captions.
        // Server speech-to-text is an optional fallback or plugin.
        return false;
    }

    public async Task<string> ProcessAudioStreamAsync(Stream audioStream, string language = "en")
    {
        _logger.LogInformation("Processing audio stream for transcription in language {Language}", language);
        await Task.Delay(10);
        return string.Empty;
    }
}
