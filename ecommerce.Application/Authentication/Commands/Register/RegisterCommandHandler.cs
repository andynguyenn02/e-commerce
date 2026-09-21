using ecommerce.Application.Common.Interfaces;
using ecommerce.Domain.Entities;
using ecommerce.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Authentication.Commands.Register;

public class RegisterCommandHandler(IAppDbContext context, IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterCommand>
{
    public async Task Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var exist = await context.Users.FirstOrDefaultAsync(u => u.UserName == request.Username, cancellationToken);
        if (exist is not null)
            throw new InvalidOperationException("Username already exists");

        var user = new UserEntity
        {
            UserName = request.Username,
            PasswordHash = passwordHasher.GenerateHash(request.Password),
            Role = RoleEnum.Customer
        };

        context.Users.Add(user);
        //add wallet with 1000 balance as default when create account
        context.Wallets.Add(new WalletEntity { UserId = user.Id });

        await context.SaveChangesAsync(cancellationToken);
    }
}