namespace ecommerce.Application.Common.Exceptions;

public class InsufficientBalanceException(decimal required, decimal balance)
    : BusinessException("INSUFFICIENT_BALANCE", $"Need {required}, have {balance}");
