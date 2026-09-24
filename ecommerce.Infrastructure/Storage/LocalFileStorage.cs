using ecommerce.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ecommerce.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    public LocalFileStorage(IConfiguration config)
    {
        var rootPath = config["Storage:rootPath"];

        _uploadPath = Path.Combine(rootPath!, "Uploads");
        Directory.CreateDirectory(_uploadPath);

        _archivePath = Path.Combine(rootPath!, "Archive");
        Directory.CreateDirectory(_archivePath);

        _failedPath = Path.Combine(rootPath!, "Failed");
        Directory.CreateDirectory(_failedPath);
    }

    private string _uploadPath { get; }
    private string _archivePath { get; }
    private string _failedPath { get; }

    public void MoveToArchive(string storedFileName)
    {
        var source = Path.Combine(_uploadPath, storedFileName);
        var destination = Path.Combine(_archivePath, storedFileName);
        File.Move(source, destination);
    }

    public void MoveToFailed(string storedFileName)
    {
        var source = Path.Combine(_uploadPath, storedFileName);
        var destination = Path.Combine(_failedPath, storedFileName);
        File.Move(source, destination);
    }

    public Stream OpenForRead(string storedFileName)
    {
        var source = Path.Combine(_uploadPath, storedFileName);
        return File.OpenRead(source);
    }

    public string SaveToUploads(Stream context, string originalFileName)
    {
        var fileNameWithNoExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var fileExtension = Path.GetExtension(originalFileName);
        var uniqueFileName = $"{Guid.NewGuid()}-{fileNameWithNoExtension}{fileExtension}";

        var path = Path.Combine(_uploadPath, uniqueFileName);

        using var fileStream = new FileStream(path, FileMode.Create);

        context.CopyTo(fileStream);

        return uniqueFileName;
    }
}