using System;

namespace ecommerce.Application.Common.Exceptions;

public class CategoryHasProductException(string name)
    : ConflictException("CATEGORY_HAS_PRODUCT", $"Category '{name}' already has product use");
