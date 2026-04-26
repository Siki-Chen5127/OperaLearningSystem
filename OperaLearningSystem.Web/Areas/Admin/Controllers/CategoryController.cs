using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Web.Areas.Admin.ViewModels;
using System.IO;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Areas.Admin.Controllers
{
    public class CategoryController : BaseAdminController
    {
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<User> _userManager;
        public CategoryController(ICategoryService categoryService, IWebHostEnvironment webHostEnvironment, UserManager<User> userManager)
        {
            _categoryService = categoryService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index(string searchString, int pageNumber = 1)
        {
            ViewData["CurrentFilter"] = searchString;
            const int pageSize = 5;

            // 1. 从服务层获取原始的分页数据
            var pagedCategories = await _categoryService.GetPagedAsync(pageNumber, pageSize, searchString);

            // 2.将原始实体列表 映射/转换 为视图模型列表
            var categoryViewModels = pagedCategories.Items.Select(c => new Core.DTOs.CategoryIndexViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ImageUrl = c.ImageUrl,
                PlayCount = c.Plays?.Count() ?? 0,
                CourseCount = c.Courses?.Count() ?? 0,
                MasterCount = c.Masters?.Count() ?? 0
            }).ToList();

            // 3. 创建一个新的、符合视图要求的 PagedResult<CategoryIndexViewModel>
            var pagedViewModelResult = new PagedResult<Core.DTOs.CategoryIndexViewModel>
            {
                Items = categoryViewModels,
                PageNumber = pagedCategories.PageNumber,
                PageSize = pagedCategories.PageSize,
                TotalItems = pagedCategories.TotalItems
            };

            // 4. 将“重新包装”好的分页数据传递给视图
            return View(pagedViewModelResult);
        }
        public IActionResult Create()
        {
            return View(new CategoryCreateViewModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                bool isSuperAdmin = await _userManager.IsInRoleAsync(currentUser, "SuperAdmin");
                string uniqueFileName = await UploadFile(viewModel.ImageFile);
                Category newCategory = new Category
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    History = viewModel.History,
                    ImageUrl = uniqueFileName,
                    SubmitterId = currentUser?.Id,
                    AuditStatus = isSuperAdmin ? 1 : 0
                };
                TempData["SuccessMessage"] = isSuperAdmin
                    ? $"剧种 “{newCategory.Name}” 发布成功！"
                    : $"剧种 “{newCategory.Name}” 已呈递！请等待掌印审核。";
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            var viewModel = new CategoryEditViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                History = category.History,
                ExistingImageUrl = category.ImageUrl
            };
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryEditViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }
            ModelState.Remove("ImageFile");
            if (ModelState.IsValid)
            {
                var categoryToUpdate = await _categoryService.GetByIdAsync(id);
                if (categoryToUpdate == null) return NotFound();
                var currentUser = await _userManager.GetUserAsync(User);
                bool isSuperAdmin = await _userManager.IsInRoleAsync(currentUser, "SuperAdmin");
                if (viewModel.ImageFile != null)
                {
                    categoryToUpdate.ImageUrl = await UploadFile(viewModel.ImageFile);
                }
                categoryToUpdate.Name = viewModel.Name;
                categoryToUpdate.Description = viewModel.Description;
                categoryToUpdate.History = viewModel.History;
                if (!isSuperAdmin)
                {
                    categoryToUpdate.AuditStatus = 0;
                    categoryToUpdate.SubmitterId = currentUser?.Id;
                }
                await _categoryService.UpdateAsync(categoryToUpdate);
                TempData["SuccessMessage"] = isSuperAdmin
                    ? $"剧种 “{categoryToUpdate.Name}” 更新成功！"
                    : $"剧种 “{categoryToUpdate.Name}” 修改已提交，重新进入待审核状态！";
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var category = await _categoryService.GetCategoryDetailsByIdAsync(id.Value);
            if (category == null) return NotFound();
            return Json(new { id = category.Id, name = category.Name });
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _categoryService.GetCategoryDetailsByIdAsync(id);
            if (category == null) return NotFound();

            if (category.Plays.Any() || category.Courses.Any() || category.Masters.Any())
            {
                TempData["ErrorMessage"] = $"无法删除剧种 “{category.Name}”，因为它已关联了剧目、课程或名家。";
                return RedirectToAction(nameof(Index));
            }

            if (!string.IsNullOrEmpty(category.ImageUrl))
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, category.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldImagePath))
                {
                    System.IO.File.Delete(oldImagePath);
                }
            }

            await _categoryService.DeleteAsync(id);
            TempData["SuccessMessage"] = $"剧种 “{category.Name}” 已被成功删除。";
            return RedirectToAction(nameof(Index));
        }
        private async Task<string> UploadFile(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return null;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "categories");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return $"/images/categories/{uniqueFileName}";
        }
    }
}