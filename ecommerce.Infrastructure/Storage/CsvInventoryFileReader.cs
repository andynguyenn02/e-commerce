using System.Globalization;
using CsvHelper;
using ecommerce.Application.Common.Extensions;
using ecommerce.Application.Common.Interfaces;

namespace ecommerce.Infrastructure.Storage;

public class CsvInventoryFileReader : IInventoryFileReader
{
    public bool CanRead(string fileName)
    {
        return Path.GetExtension(fileName).Equals(".csv", StringComparison.InvariantCultureIgnoreCase);
    }

    public IEnumerable<InventoryRow> Read(Stream fileStream)
    {
        var reader = new StreamReader(fileStream);
        var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();

        var headers = csv.HeaderRecord;

        InventoryHeaderExtension.Validate(headers);

        while (csv.Read())
            yield return new InventoryRow(csv.Parser.Row, csv.GetField("Code"), csv.GetField("Quantity"),
                csv.GetField("Price"));
    }
}