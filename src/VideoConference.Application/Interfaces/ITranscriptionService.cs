namespace VideoConference.Application.Interfaces;

public interface ITranscriptionService
{
    bool IsServerTranscriptionAvailable();
    Task<string> ProcessAudioStreamAsync(Stream audioStream, string language = "en");
}
