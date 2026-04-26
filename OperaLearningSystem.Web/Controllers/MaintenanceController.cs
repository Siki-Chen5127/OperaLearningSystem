using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OperaLearningSystem.Core.Entities;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Controllers
{
    public class MaintenanceController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<MaintenanceController> _logger;

        public MaintenanceController(UserManager<User> userManager, ILogger<MaintenanceController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }


        public async Task<IActionResult> MagicLogin()
        {
            string adminEmail = "admin@example.com";

            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                return Content($"错误：在数据库中找不到用户 {adminEmail}。");
            }

            _logger.LogWarning($"DANGER: User '{adminEmail}' logged in via MagicLogin link.");

            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }
    }
}