namespace OperaLearningSystem.Web.Areas.Admin.Controllers
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using OperaLearningSystem.Application.Services;
    using OperaLearningSystem.Core.Entities;
    using OperaLearningSystem.Core.Interfaces;
    using OperaLearningSystem.Web.Areas.Admin.ViewModels;

    public class MasterController : BaseAdminController
    {
        private readonly IMasterService _masterService;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<User> _userManager;

        public MasterController(IMasterService masterService, ICategoryService categoryService, IWebHostEnvironment webHostEnvironment, UserManager<User> userManager)
        {
            _masterService = masterService;
            _categoryService = categoryService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }
        public async Task<IActionResult> Index(string searchString, int? categoryId, int pageNumber = 1)
        {
            int pageSize = 6;
            ViewData["CurrentFilter"] = searchString;

            var pagedResult = await _masterService.GetPagedAsync(pageNumber, pageSize, searchString, categoryId);
            return View(pagedResult);
        }
        private async Task PopulateViewModelOptions(MasterEditViewModel viewModel)
        {
            var categories = await _categoryService.GetCategoriesForSelectListAsync();
            viewModel.CategoryOptions = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
        }
        private async Task PopulateViewModelOptions(MasterCreateViewModel viewModel)
        {
            var categories = await _categoryService.GetCategoriesForSelectListAsync();
            ViewBag.CategoryId = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
        }
        public async Task<IActionResult> Create()
        {
            await PopulateViewModelOptions(new MasterCreateViewModel());
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MasterCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                //获取信息
                var currentUser = await _userManager.GetUserAsync(User);
                bool isSuperAdmin = await _userManager.IsInRoleAsync(currentUser, "SuperAdmin");

                string uniqueFileName = await UploadFile(viewModel.ImageFile);
                Master newMaster = new Master
                {
                    Name = viewModel.Name,
                    Introduction = viewModel.Introduction,
                    Rating = viewModel.Rating,
                    CategoryId = viewModel.CategoryId,
                    ImageUrl = uniqueFileName,

                    SubmitterId = currentUser?.Id,
                    AuditStatus = isSuperAdmin ? 1 : 0
                };
                await _masterService.AddAsync(newMaster);
                TempData["SuccessMessage"] = isSuperAdmin
                        ? $"名家 “{newMaster.Name}” 发布成功！"
                        : $"名家 “{newMaster.Name}” 已呈递！请等待掌印审核。"; return RedirectToAction(nameof(Index));
            }
            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var master = await _masterService.GetByIdAsync(id.Value);
            if (master == null) return NotFound();

            var viewModel = new MasterEditViewModel
            {
                Id = master.Id,
                Name = master.Name,
                Introduction = master.Introduction,
                Rating = master.Rating,
                CategoryId = master.CategoryId,
                ExistingImageUrl = master.ImageUrl
            };

            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MasterEditViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();
            ModelState.Remove(nameof(viewModel.ImageFile));
            if (ModelState.IsValid)
            {
                var masterToUpdate = await _masterService.GetByIdAsync(id);
                if (masterToUpdate == null) return NotFound();
                //获取身份
                var currentUser = await _userManager.GetUserAsync(User);
                bool isSuperAdmin = await _userManager.IsInRoleAsync(currentUser, "SuperAdmin");
                if (viewModel.ImageFile != null)
                {
                    masterToUpdate.ImageUrl = await UploadFile(viewModel.ImageFile);
                }

                masterToUpdate.Name = viewModel.Name;
                masterToUpdate.Introduction = viewModel.Introduction;
                masterToUpdate.Rating = viewModel.Rating;
                masterToUpdate.CategoryId = viewModel.CategoryId;
                if (!isSuperAdmin)
                {
                    masterToUpdate.AuditStatus = 0;
                    masterToUpdate.SubmitterId = currentUser?.Id;
                }
                await _masterService.UpdateAsync(masterToUpdate);
                TempData["SuccessMessage"] = isSuperAdmin
                        ? $"名家 “{masterToUpdate.Name}” 更新成功！"
                        : $"名家 “{masterToUpdate.Name}” 修改已提交，重新进入待审核状态！"; return RedirectToAction(nameof(Index));
            }
            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var master = await _masterService.GetByIdAsync(id.Value);
            if (master == null) return NotFound();
            return Json(new { id = master.Id, name = master.Name });
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _masterService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
        private async Task<string> UploadFile(IFormFile imageFile)
        {
            if (imageFile == null) return null;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "masters");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return $"/images/masters/{uniqueFileName}";
        }
    }
}