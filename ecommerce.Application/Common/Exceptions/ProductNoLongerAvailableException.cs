namespace ecommerce.Application.Common.Exceptions;

public class ProductNoLongerAvailableException(IEnumerable<string> names)
    : BusinessException("PRODUCT_NO_LONGER_AVAILABLE", $"No longer available: {string.Join(", ", names)}");
