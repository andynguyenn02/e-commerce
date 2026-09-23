namespace ecommerce.Application.Common.Exceptions;

public abstract class ConflictException(string code, string message)
    : BusinessException(code, message);
