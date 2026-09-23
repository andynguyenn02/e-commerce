namespace ecommerce.Application.Common.Exceptions;

public class InvalidCredentialsException()
    : BusinessException("INVALID_CREDENTIALS", "Username or password is incorrect");
