using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Community")]
    public class CommunityPostController : BaseAdminController
    {
        private readonly ICommunityPostService _postService;
        public CommunityPostController(ICommunityPostService postService, OperaDbContext context)
        {
            _postService = postService;
        }
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1)
        {
            ViewData["CurrentFilter"] = searchString;

            const int pageSize = 5;

            var pagedResult = await _postService.GetPagedAsync(pageNumber, pageSize, searchString, null);
            return View(pagedResult);
        }
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var post = await _postService.GetByIdAsync(id);

            if (post == null)
            {
                return NotFound();
            }
            return View(post);
        }
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var postToDelete = await _postService.GetByIdAsync(id);
            if (postToDelete == null)
            {
                TempData["ErrorMessage"] = "要删除的帖子不存在。";
                return RedirectToAction(nameof(Index));
            }
            await _postService.DeleteAsync(id);
            TempData["SuccessMessage"] = $"帖子 “{postToDelete.Title}” 已成功删除。";
            return RedirectToAction(nameof(Index));
        }
    }
}