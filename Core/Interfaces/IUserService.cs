namespace OperaLearningSystem.Core.Interfaces;

using Microsoft.AspNetCore.Identity;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;

public interface IUserService
{
    Task<User> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<PagedResult<User>> GetPagedAsync(int pageNumber, int pageSize, string searchString);
    Task<int> GetUserCountAsync();
    Task<IEnumerable<User>> GetRecentUsersAsync(int count);
    Task<IList<string>> GetUserRolesAsync(User user);
    Task<IdentityResult> AddUserToRoleAsync(User user, string roleName);
    Task<IdentityResult> RemoveUserFromRoleAsync(User user, string roleName);
    Task<IdentityResult> LockoutUserAsync(User user, bool lockUser);
    Task<IEnumerable<IdentityRole<int>>> GetAllRolesAsync();
}