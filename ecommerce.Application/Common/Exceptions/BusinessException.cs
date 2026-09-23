namespace ecommerce.Application.Common.Exceptions;

public abstract class BusinessException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}