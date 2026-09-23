namespace ecommerce.Application.Common.Exceptions;

public class CategoryAlreadyExistsException(string name)
    : ConflictException("CATEGORY_ALREADY_EXISTS", $"Category '{name}' already exists");
