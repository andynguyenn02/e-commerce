using System.Globalization;
using ClosedXML.Excel;
using ecommerce.Application.Common.Extensions;
using ecommerce.Application.Common.Interfaces;

namespace ecommerce.Infrastructure.Storage;

public class ExcelInventoryFileReader : IInventoryFileReader
{
    public bool CanRead(string fileName)
    {
        return Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerable<InventoryRow> Read(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        var headerRow = sheet.FirstRowUsed()
                        ?? throw new Exception("The Excel file is empty");

        var columns = headerRow.CellsUsed()
            .ToDictionary(c => c.GetString().Trim(), c => c.Address.ColumnNumber);

        InventoryHeaderExtension.Validate(columns.Keys);

        foreach (var row in sheet.RowsUsed().Skip(1))
            yield return new InventoryRow(
                row.RowNumber(),
                row.Cell(columns["Code"]).GetString().Trim(),
                CellToString(row.Cell(columns["Quantity"])),
                CellToString(row.Cell(columns["Price"])));
    }

    // Numeric cells: avoid locale/format issues (e.g. "1,234.50" or "$10")
    private static string CellToString(IXLCell cell)
    {
        return cell.DataType == XLDataType.Number
            ? cell.GetDouble().ToString(CultureInfo.InvariantCulture)
            : cell.GetString().Trim();
    }
}