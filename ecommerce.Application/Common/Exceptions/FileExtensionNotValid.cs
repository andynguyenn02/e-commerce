namespace ecommerce.Application.Common.Exceptions;

public class FileExtensionNotValid(string name)
    : ConflictException("EXTENSION_INVALID", $"File extension '{name}' not valid");