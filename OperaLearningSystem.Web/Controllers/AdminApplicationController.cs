using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Controllers
{
    [Authorize] // 必须登录才能申请
    public class AdminApplicationController : Controller
    {
        private readonly OperaDbContext _context;
        private readonly UserManager<User> _userManager;

        public AdminApplicationController(OperaDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. 展示申请页面
        [HttpGet]
        public async Task<IActionResult> Apply()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // 如果已经是 Admin 或 SuperAdmin，直接劝退
            if (await _userManager.IsInRoleAsync(currentUser, "Admin") ||
                await _userManager.IsInRoleAsync(currentUser, "SuperAdmin"))
            {
                ViewBag.Message = "您已是尊贵的梨园管理员，无需再次申请。";
                ViewBag.Status = "AlreadyAdmin";
                return View();
            }

            // 查询用户最近的一次申请记录
            var latestApplication = await _context.AdminApplications
                .Where(a => a.UserId == currentUser.Id)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            return View(latestApplication);
        }

        // 2. 接收用户提交的申请表单
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitApply(string reason)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
            {
                TempData["ErrorMessage"] = "申请理由过于简短，请多说几句您的戏曲情怀（至少10个字）";
                return RedirectToAction(nameof(Apply));
            }

            // 防止用户狂点按钮重复提交
            var hasPending = await _context.AdminApplications
                .AnyAsync(a => a.UserId == currentUser.Id && a.Status == 0);

            if (hasPending)
            {
                TempData["ErrorMessage"] = "您已有一份待审核的申请，请勿重复提交。";
                return RedirectToAction(nameof(Apply));
            }

            // 生成新的申请记录存入数据库
            var application = new AdminApplication
            {
                UserId = currentUser.Id,
                Reason = reason,
                Status = 0, // 0 = 待审核
                CreatedAt = DateTime.Now
            };

            _context.AdminApplications.Add(application);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "您的“入园名刺”已递交，请静候佳音！";
            return RedirectToAction(nameof(Apply));
        }
    }
}