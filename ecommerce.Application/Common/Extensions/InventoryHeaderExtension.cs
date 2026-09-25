namespace ecommerce.Application.Common.Extensions;

public class InventoryHeaderExtension
{
    private static readonly string[] Expected = ["Code", "Quantity", "Price"];

    public static void Validate(IEnumerable<string>? headers)
    {
        var actual = headers?.Select(h => h.Trim()).ToArray() ?? [];
        if (!Expected.ToHashSet().SetEquals(actual))
            throw new Exception(
                $"Invalid header format. Expected: [{string.Join(", ", Expected)}], " +
                $"Actual: [{string.Join(", ", actual)}]");
    }
}