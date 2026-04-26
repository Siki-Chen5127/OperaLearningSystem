using Microsoft.AspNetCore.Mvc;
using OperaLearningSystem.Core.Interfaces;

namespace OperaLearningSystem.Web.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public async Task<IActionResult> Index(string searchString, int pageNumber = 1)
        {
            int pageSize = 6; // 每页显示6个剧种
            ViewData["CurrentFilter"] = searchString;
            var pagedResult = await _categoryService.GetPagedAsync(pageNumber, pageSize, searchString, true);
            return View(pagedResult);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _categoryService.GetCategoryDetailsByIdAsync(id.Value);

            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }
    }
}