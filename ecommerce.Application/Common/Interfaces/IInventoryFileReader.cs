namespace ecommerce.Application.Common.Interfaces;

public record InventoryRow(int RowNumber, string? Code, string? Quantity, string? Price);

public interface IInventoryFileReader
{
    bool CanRead(string fileName);
    IEnumerable<InventoryRow> Read(Stream fileStream);
}