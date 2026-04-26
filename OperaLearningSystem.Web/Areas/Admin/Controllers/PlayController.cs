using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Web.Areas.Admin.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace OperaLearningSystem.Web.Areas.Admin.Controllers
{
    public class PlayController : BaseAdminController
    {
        private readonly IPlayService _playService;
        private readonly ICategoryService _categoryService;
        private readonly IMasterService _masterService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<User> _userManager;

        public PlayController(
            IPlayService playService,
            ICategoryService categoryService,
            IMasterService masterService,
            IWebHostEnvironment webHostEnvironment,
            UserManager<User> userManager) // 注入
        {
            _playService = playService;
            _categoryService = categoryService;
            _masterService = masterService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string searchString, int pageNumber = 1)
        {
            ViewData["CurrentFilter"] = searchString;

            const int pageSize = 6;

            // 1. 从服务层获取分页数据
            var pagedPlays = await _playService.GetPagedAsync(pageNumber, pageSize, searchString, null);

            // 2. 映射为视图模型
            var playViewModels = pagedPlays.Items.Select(p => new PlayIndexViewModel
            {
                Id = p.Id,
                Title = p.Title,
                ImageUrl = p.ImageUrl,
                CategoryName = p.Category?.Name,
                MasterNames = p.PlayMasters.Select(pm => pm.Master?.Name).ToList()
            }).ToList();

            // 3. 重新包装返回给视图
            var pagedViewModelResult = new PagedResult<PlayIndexViewModel>
            {
                Items = playViewModels,
                PageNumber = pagedPlays.PageNumber,
                PageSize = pagedPlays.PageSize,
                TotalItems = pagedPlays.TotalItems
            };

            return View(pagedViewModelResult);
        }

        private async Task PopulateViewModelOptions(PlayViewModel viewModel)
        {
            var categories = await _categoryService.GetCategoriesForSelectListAsync();
            var masters = await _masterService.GetAllAsync();

            viewModel.CategoryOptions = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
            viewModel.MasterOptions = new MultiSelectList(masters, "Id", "Name", viewModel.SelectedMasterIds);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new PlayViewModel();
            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlayViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // 获取当前用户和他的最高权限
                var currentUser = await _userManager.GetUserAsync(User);
                bool isSuperAdmin = await _userManager.IsInRoleAsync(currentUser, "SuperAdmin");

                string uniqueFileName = await UploadFile(viewModel.ImageFile);
                var play = new Play
                {
                    Title = viewModel.Title,
                    Synopsis = viewModel.Synopsis,
                    VideoUrl = viewModel.VideoUrl,
                    CategoryId = viewModel.CategoryId,
                    ImageUrl = uniqueFileName,
                    SubmitterId = currentUser?.Id,
                    AuditStatus = isSuperAdmin ? 1 : 0 // 站长直接是1(已发布)，共创者是0(待审)
                };

                await _playService.AddAsync(play);
                await _playService.UpdatePlayMastersAsync(play.Id, viewModel.SelectedMasterIds ?? new List<int>());

                //  根据身份给出不同的提示语
                TempData["SuccessMessage"] = isSuperAdmin
                    ? $"剧目 “{play.Title}” 发布成功！"
                    : $"剧目 “{play.Title}” 已呈递！请等待掌印审核后方可对外展示。";

                return RedirectToAction(nameof(Index));
            }

            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var play = await _playService.GetByIdAsync(id.Value);
            if (play == null) return NotFound();

            var viewModel = new PlayViewModel
            {
                Id = play.Id,
                Title = play.Title,
                Synopsis = play.Synopsis,
                VideoUrl = play.VideoUrl,
                CategoryId = play.CategoryId,
                SelectedMasterIds = play.PlayMasters.Select(pm => pm.MasterId).ToList(),
                ExistingImageUrl = play.ImageUrl
            };

            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PlayViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            ModelState.Remove(nameof(viewModel.ImageFile));
            ModelState.Remove(nameof(viewModel.SelectedMasterIds));

            if (ModelState.IsValid)
            {
                var playToUpdate = await _playService.GetByIdAsync(id);
                if (playToUpdate == null) return NotFound();

                // 获取当前用户身份
                var currentUser = await _userManager.GetUserAsync(User);
                bool isSuperAdmin = await _userManager.IsInRoleAsync(currentUser, "SuperAdmin");

                if (viewModel.ImageFile != null && viewModel.ImageFile.Length > 0)
                {
                    playToUpdate.ImageUrl = await UploadFile(viewModel.ImageFile);
                }

                playToUpdate.Title = viewModel.Title;
                playToUpdate.Synopsis = viewModel.Synopsis;
                playToUpdate.VideoUrl = viewModel.VideoUrl;
                playToUpdate.CategoryId = viewModel.CategoryId;

                // 共创者修改了已发布的内容，必须重新打回待审核状态
                if (!isSuperAdmin)
                {
                    playToUpdate.AuditStatus = 0;
                    playToUpdate.SubmitterId = currentUser?.Id; // 更新最后修改人为当前共创者
                }

                await _playService.UpdatePlayMastersAsync(playToUpdate.Id, viewModel.SelectedMasterIds ?? new List<int>());
                await _playService.UpdateAsync(playToUpdate);

                //  根据身份给出不同的提示语
                TempData["SuccessMessage"] = isSuperAdmin
                    ? $"剧目 “{playToUpdate.Title}” 更新成功！"
                    : $"剧目 “{playToUpdate.Title}” 修改已提交，重新进入待审核状态！";

                return RedirectToAction(nameof(Index));
            }

            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var playToDelete = await _playService.GetByIdAsync(id);
            if (playToDelete == null) return NotFound();

            await _playService.DeleteAsync(id);
            TempData["SuccessMessage"] = $"剧目 “{playToDelete.Title}” 已成功删除。";
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "plays");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }
            return $"/images/plays/{uniqueFileName}";
        }
    }
}