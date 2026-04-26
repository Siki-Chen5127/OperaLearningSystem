using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Web.Areas.Admin.ViewModels;

namespace OperaLearningSystem.Web.Areas.Admin.Controllers
{
    public class AiCharacterController : BaseAdminController
    {
        private readonly OperaDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AiCharacterController(OperaDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(int pageNumber = 1)
        {
            const int pageSize = 6; 

            var query = _context.AiCharacters.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Id);

            var totalItems = await query.CountAsync();
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            // 包装成和之前一样的 PagedResult
            var pagedResult = new PagedResult<AiCharacter>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(pagedResult);
        }

        public IActionResult Create() => View(new AiCharacterFormViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AiCharacterFormViewModel vm)
        {
            vm.Id = null;
            ModelState.Remove(nameof(vm.Id));
            if (!ModelState.IsValid) return View(vm);

            var entity = new AiCharacter
            {
                Name = vm.Name.Trim(),
                Description = vm.Description?.Trim() ?? string.Empty,
                SystemPrompt = vm.SystemPrompt.Trim(),
                GreetingMessage = vm.GreetingMessage?.Trim() ?? string.Empty,
                IsActive = vm.IsActive,
                SortOrder = vm.SortOrder,
                AvatarUrl = (await UploadIfAny(vm.AvatarFile)) ?? vm.AvatarUrl?.Trim() ?? string.Empty,
                BackgroundUrl = (await UploadIfAny(vm.BackgroundFile)) ?? vm.BackgroundUrl?.Trim() ?? string.Empty,
            };
            _context.AiCharacters.Add(entity);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"角色「{entity.Name}」已创建。";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var ch = await _context.AiCharacters.FindAsync(id);
            if (ch == null) return NotFound();

            var vm = new AiCharacterFormViewModel
            {
                Id = ch.Id,
                Name = ch.Name,
                Description = ch.Description,
                AvatarUrl = ch.AvatarUrl,
                BackgroundUrl = ch.BackgroundUrl,
                SystemPrompt = ch.SystemPrompt,
                GreetingMessage = ch.GreetingMessage,
                IsActive = ch.IsActive,
                SortOrder = ch.SortOrder,
                ExistingAvatarDisplay = ch.AvatarUrl,
                ExistingBackgroundDisplay = ch.BackgroundUrl,
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AiCharacterFormViewModel vm)
        {
            if (id != vm.Id) return NotFound();
            ValidateUrls(vm);
            if (!ModelState.IsValid) return View(vm);

            var ch = await _context.AiCharacters.FindAsync(id);
            if (ch == null) return NotFound();

            ch.Name = vm.Name.Trim();
            ch.Description = vm.Description?.Trim() ?? string.Empty;
            ch.SystemPrompt = vm.SystemPrompt.Trim();
            ch.GreetingMessage = vm.GreetingMessage?.Trim() ?? string.Empty;
            ch.IsActive = vm.IsActive;
            ch.SortOrder = vm.SortOrder;

            if (vm.AvatarFile is { Length: > 0 })
            {
                DeleteWebFile(ch.AvatarUrl);
                ch.AvatarUrl = (await UploadIfAny(vm.AvatarFile)) ?? string.Empty;
            }
            else
            {
                ch.AvatarUrl = vm.AvatarUrl?.Trim() ?? string.Empty;
            }

            if (vm.BackgroundFile is { Length: > 0 })
            {
                DeleteWebFile(ch.BackgroundUrl);
                ch.BackgroundUrl = (await UploadIfAny(vm.BackgroundFile)) ?? string.Empty;
            }
            else
            {
                ch.BackgroundUrl = vm.BackgroundUrl?.Trim() ?? string.Empty;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"角色「{ch.Name}」已保存。";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ch = await _context.AiCharacters.FindAsync(id);
            if (ch == null)
            {
                TempData["ErrorMessage"] = "角色不存在。";
                return RedirectToAction(nameof(Index));
            }

            _context.AiCharacters.Remove(ch);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"角色「{ch.Name}」已删除（含关联对话记录）。";
            return RedirectToAction(nameof(Index));
        }

        private void ValidateUrls(AiCharacterFormViewModel vm)
        {
            if (vm.AvatarUrl != null && vm.AvatarUrl.Length > 255)
                ModelState.AddModelError(nameof(vm.AvatarUrl), "路径过长");
            if (vm.BackgroundUrl != null && vm.BackgroundUrl.Length > 255)
                ModelState.AddModelError(nameof(vm.BackgroundUrl), "路径过长");
        }

        private async Task<string?> UploadIfAny(IFormFile? file)
        {
            if (file == null || file.Length == 0) return null;
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext)) ext = ".png";
            var folder = Path.Combine(_env.WebRootPath, "images", "ai");
            Directory.CreateDirectory(folder);
            var name = Guid.NewGuid().ToString("N") + ext;
            var path = Path.Combine(folder, name);
            await using (var fs = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }
            return $"/images/ai/{name}";
        }

        private void DeleteWebFile(string? webPath)
        {
            if (string.IsNullOrEmpty(webPath) || !webPath.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
                return;
            var physical = Path.Combine(_env.WebRootPath, webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(physical))
            {
                try { System.IO.File.Delete(physical); } catch { /* ignore */ }
            }
        }
    }
}
