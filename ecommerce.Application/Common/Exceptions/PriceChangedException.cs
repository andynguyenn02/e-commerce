namespace ecommerce.Application.Common.Exceptions;

public class PriceChangedException()
    : ConflictException("PRICE_CHANGED", "Price changed since you loaded it.");
