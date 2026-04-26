using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OperaLearningSystem.Application.Services;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;

namespace OperaLearningSystem.Web.Controllers
{
    public class MasterController : Controller
    {
        private readonly IMasterService _masterService;
        private readonly UserManager<User> _userManager;

        public MasterController(IMasterService masterService, UserManager<User> userManager)
        {
            _masterService = masterService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string searchString, int? categoryId, int pageNumber = 1)
        {
            int pageSize = 6;
            ViewData["CurrentFilter"] = searchString;
            ViewData["CategoryId"] = categoryId;
            var pagedResult = await _masterService.GetPagedAsync(pageNumber, pageSize, searchString, categoryId, true);
            return View(pagedResult);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var master = await _masterService.GetByIdAsync(id.Value);

            if (master == null) return NotFound();

            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(_userManager.GetUserId(User));
                ViewBag.IsLikedByCurrentUser = master.Likes.Any(l => l.UserId == userId);
                ViewBag.IsFavoritedByCurrentUser = master.Favorites.Any(f => f.UserId == userId);
            }
            else
            {
                ViewBag.IsLikedByCurrentUser = false;
                ViewBag.IsFavoritedByCurrentUser = false;
            }

            return View(master);
        }
    }
}