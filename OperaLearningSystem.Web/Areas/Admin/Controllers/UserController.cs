using Microsoft.AspNetCore.Mvc;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Users")] 
    public class UserController : BaseAdminController
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("")] 
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1, int pageSize = 10)
        {
            ViewData["CurrentFilter"] = searchString;
            var pagedResult = await _userService.GetPagedAsync(pageNumber, pageSize, searchString);
            return View(pagedResult);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.UserRoles = await _userService.GetUserRolesAsync(user);
            ViewBag.AllRoles = await _userService.GetAllRolesAsync();

            return View(user);
        }

        [HttpPost("ToggleLockout/{id}")] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLockout(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null) return NotFound();

            bool isCurrentlyLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
            var result = await _userService.LockoutUserAsync(user, !isCurrentlyLocked);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"用户 “{user.UserName}” 的状态已更新。";
            }
            else
            {
                TempData["ErrorMessage"] = "操作失败，请重试。";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost("AssignRole")] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int userId, string roleName)
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userService.AddUserToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"已成功为 “{user.UserName}” 分配角色 “{roleName}”。";
            }
            return RedirectToAction(nameof(Details), new { id = userId });
        }

        [HttpPost("RemoveRole")] 
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(int userId, string roleName)
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userService.RemoveUserFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"已成功从 “{user.UserName}” 移除角色 “{roleName}”。";
            }
            return RedirectToAction(nameof(Details), new { id = userId });
        }
    }
}