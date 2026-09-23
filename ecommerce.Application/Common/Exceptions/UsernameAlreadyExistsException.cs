namespace ecommerce.Application.Common.Exceptions;

public class UsernameAlreadyExistsException(string username)
    : ConflictException("USERNAME_ALREADY_EXISTS", $"Username '{username}' already exists");
