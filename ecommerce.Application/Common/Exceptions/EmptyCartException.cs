namespace ecommerce.Application.Common.Exceptions;

public class EmptyCartException()
    : BusinessException("CART_EMPTY", "No items found in cart");
