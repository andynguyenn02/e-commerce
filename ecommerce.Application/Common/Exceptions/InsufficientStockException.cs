namespace ecommerce.Application.Common.Exceptions;

public class InsufficientStockException(string productName, int available)
    : BusinessException("INSUFFICIENT_STOCK", $"{productName}: only {available} left");
