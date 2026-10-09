namespace VideoConference.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(string subDirectory, string fileName, Stream content);
    Task<Stream?> GetFileAsync(string storagePath);
    Task<bool> DeleteFileAsync(string storagePath);
    bool FileExists(string storagePath);
}
