using ecommerce.Domain.Entities;

namespace ecommerce.Domain.Interfaces;

public interface IUserRepository
{
    public Task<List<UserEntity>> GetAllUsers();
    public Task<UserEntity?> GetUserById(Guid userId);
    public Task UpdateUser(UserEntity user);
    public Task DeleteUser(UserEntity user);
    public Task<UserEntity> CreateUser(UserEntity user);
    public Task<UserEntity?> GetUserByUserName(string username);
}