namespace OperaLearningSystem.Application.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<int>> _roleManager; // 新增：注入 RoleManager

    public UserService(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }
    public async Task<PagedResult<User>> GetPagedAsync(int pageNumber, int pageSize, string searchString)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(u => u.UserName.Contains(searchString) || u.Email.Contains(searchString));
        }

        var totalItems = await query.CountAsync();
        var items = await query.OrderBy(u => u.UserName)
                               .Skip((pageNumber - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();
        return new PagedResult<User> { Items = items, TotalItems = totalItems, PageNumber = pageNumber, PageSize = pageSize };
    }
    public async Task<IList<string>> GetUserRolesAsync(User user)
    {
        return await _userManager.GetRolesAsync(user);
    }
    public async Task<IdentityResult> AddUserToRoleAsync(User user, string roleName)
    {
        return await _userManager.AddToRoleAsync(user, roleName);
    }
    public async Task<IdentityResult> RemoveUserFromRoleAsync(User user, string roleName)
    {
        return await _userManager.RemoveFromRoleAsync(user, roleName);
    }
    public async Task<IdentityResult> LockoutUserAsync(User user, bool shouldLock)
    {
        if (shouldLock)
        {
            return await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        }
        else
        {
            return await _userManager.SetLockoutEndDateAsync(user, null);
        }
    }
    public async Task<IEnumerable<IdentityRole<int>>> GetAllRolesAsync()
    {
        return await _roleManager.Roles.ToListAsync();
    }
    public async Task<User> GetByIdAsync(int id) => await _userManager.FindByIdAsync(id.ToString());
    public async Task<IEnumerable<User>> GetAllAsync() => await _userManager.Users.ToListAsync();
    public async Task<int> GetUserCountAsync() => await _userManager.Users.CountAsync();
    public async Task<IEnumerable<User>> GetRecentUsersAsync(int count) => await _userManager.Users.OrderByDescending(u => u.CreatedAt).Take(count).ToListAsync();
}