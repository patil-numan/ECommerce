using ECommerce.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ECommerce.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _uploadDirectory;

    public FileStorageService(
        IConfiguration configuration)
    {
        var configuredDirectory =
            configuration[
                "BulkImport:UploadDirectory"];

        if (string.IsNullOrWhiteSpace(
                configuredDirectory))
        {
            configuredDirectory =
                "Uploads/BulkImports";
        }

        _uploadDirectory =
            Path.GetFullPath(
                configuredDirectory);

        Directory.CreateDirectory(
            _uploadDirectory);
    }

    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName)
    {
        var extension =
            Path.GetExtension(fileName);

        var storedFileName =
            $"{Guid.NewGuid():N}{extension}";

        var filePath =
            Path.Combine(
                _uploadDirectory,
                storedFileName);

        await using var outputStream =
            new FileStream(
                filePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true);

        await fileStream.CopyToAsync(
            outputStream);

        return filePath;
    }

    public Task<Stream> OpenFileAsync(
        string filePath)
    {
        Stream stream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteFileAsync(
        string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }
}