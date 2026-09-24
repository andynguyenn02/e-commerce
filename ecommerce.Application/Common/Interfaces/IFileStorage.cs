namespace ecommerce.Application.Common.Interfaces;

public interface IFileStorage
{
    public string SaveToUploads(Stream context, string originalFileName);

    public void MoveToArchive(string storedFileName);
    public void MoveToFailed(string storedFileName);
    public Stream OpenForRead(string storedFileName);
}