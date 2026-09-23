namespace ecommerce.Application.Common.Exceptions;

public class NotFoundException(string resource)
    : BusinessException("NOT_FOUND", $"{resource} not found");
