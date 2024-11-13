using KMG.Repository.Models;

public interface IUserService
{
    Task<User> GetUserByIdAsync(int userId);
}


