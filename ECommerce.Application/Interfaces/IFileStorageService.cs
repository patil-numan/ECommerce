namespace ECommerce.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(
        Stream fileStream,
        string fileName);

    Task<Stream> OpenFileAsync(
        string filePath);

    Task DeleteFileAsync(
        string filePath);
}