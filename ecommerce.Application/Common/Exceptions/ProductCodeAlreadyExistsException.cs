namespace ecommerce.Application.Common.Exceptions;

public class ProductCodeAlreadyExistsException(string code)
    : ConflictException("PRODUCT_CODE_ALREADY_EXISTS", $"Product code '{code}' already exists");
