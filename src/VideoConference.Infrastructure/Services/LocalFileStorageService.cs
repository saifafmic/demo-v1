using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VideoConference.Application.Interfaces;

namespace VideoConference.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _logger = logger;
        _basePath = configuration["Storage:BasePath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
        
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }

        var recordingsPath = Path.Combine(_basePath, "recordings");
        if (!Directory.Exists(recordingsPath))
        {
            Directory.CreateDirectory(recordingsPath);
        }

        var filesPath = Path.Combine(_basePath, "meeting-files");
        if (!Directory.Exists(filesPath))
        {
            Directory.CreateDirectory(filesPath);
        }
    }

    public async Task<string> SaveFileAsync(string subDirectory, string fileName, Stream content)
    {
        var targetDir = Path.Combine(_basePath, subDirectory);
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var uniqueFileName = $"{Guid.NewGuid():N}_{fileName}";
        var fullPath = Path.Combine(targetDir, uniqueFileName);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await content.CopyToAsync(fileStream);

        _logger.LogInformation("File saved locally: {FullPath}", fullPath);
        return fullPath;
    }

    public Task<Stream?> GetFileAsync(string storagePath)
    {
        if (!File.Exists(storagePath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(storagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task<bool> DeleteFileAsync(string storagePath)
    {
        if (File.Exists(storagePath))
        {
            File.Delete(storagePath);
            _logger.LogInformation("File deleted: {StoragePath}", storagePath);
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public bool FileExists(string storagePath)
    {
        return File.Exists(storagePath);
    }
}
