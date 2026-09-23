using ecommerce.Application.Common.Exceptions;
using ecommerce.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Authentication.Commands.Login;

public class LogicCommandHandler(IAppDbContext context, IPasswordHasher hasher, IJwtService jwtService)
    : IRequestHandler<LoginCommand, string>
{
    public async Task<string> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.UserName == request.Username, cancellationToken);

        if (user == null) throw new InvalidCredentialsException();

        if (!hasher.VerifyHash(user.PasswordHash, request.Password))
            throw new InvalidCredentialsException();

        var token = jwtService.GenerateToken(user.Id, user.UserName, user.Role.ToString());

        return token;
    }
}