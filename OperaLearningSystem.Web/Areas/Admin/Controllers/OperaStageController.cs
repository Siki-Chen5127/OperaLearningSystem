using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Web.Areas.Admin.ViewModels;
using System.IO;

namespace OperaLearningSystem.Web.Areas.Admin.Controllers;

public class OperaStageController : BaseAdminController
{
    private readonly IOperaStageService _svc;
    private readonly IWebHostEnvironment _env;
    private readonly UserManager<User> _userManager;

    public OperaStageController(IOperaStageService svc, IWebHostEnvironment env, UserManager<User> userManager)
    {
        _svc = svc;
        _env = env;
        _userManager = userManager;
    }

    async Task PopulateRegions(OperaStageViewModel vm)
    {
        var regions = await _svc.GetAllRegionsAsync();
        vm.RegionOptions = regions.Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name }).ToList();
    }

    public async Task<IActionResult> Index(int? regionId)
    {
        var regions = await _svc.GetAllRegionsAsync();
        ViewBag.Regions = regions;
        if (!regionId.HasValue && regions.Count > 0)
            regionId = regions[0].Id;
        if (!regionId.HasValue)
            return View(Array.Empty<OperaStage>());
        var stages = await _svc.GetStagesByRegionAsync(regionId.Value, includeAllAudit: true);
        ViewBag.SelectedRegionId = regionId;
        return View(stages);
    }

    public async Task<IActionResult> Create(int regionId)
    {
        if (regionId <= 0 || await _svc.GetRegionByIdAsync(regionId) == null)
            return RedirectToAction(nameof(Index));
        var vm = new OperaStageViewModel { RegionId = regionId, SortOrder = 0 };
        await PopulateRegions(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OperaStageViewModel vm)
    {
        await PopulateRegions(vm);
        if (!ModelState.IsValid) return View(vm);
        var user = await _userManager.GetUserAsync(User);
        var isSuper = user != null && await _userManager.IsInRoleAsync(user, "SuperAdmin");
        var img = await UploadStageImage(vm.ImageFile);
        var stage = new OperaStage
        {
            RegionId = vm.RegionId,
            Name = vm.Name,
            Introduction = vm.Introduction,
            SortOrder = vm.SortOrder,
            ImageUrl = img ?? "/images/default.png",
            SubmitterId = user?.Id,
            AuditStatus = isSuper ? 1 : 0
        };
        await _svc.AddStageAsync(stage);
        TempData["SuccessMessage"] = isSuper ? $"戏台 “{stage.Name}” 已发布。" : "已提交审核。";
        return RedirectToAction(nameof(Index), new { regionId = vm.RegionId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var s = await _svc.GetStageByIdAsync(id);
        if (s == null) return NotFound();
        var vm = new OperaStageViewModel
        {
            Id = s.Id,
            RegionId = s.RegionId,
            Name = s.Name,
            Introduction = s.Introduction,
            SortOrder = s.SortOrder,
            ExistingImageUrl = s.ImageUrl
        };
        await PopulateRegions(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OperaStageViewModel vm)
    {
        await PopulateRegions(vm);
        ModelState.Remove("ImageFile");
        if (!ModelState.IsValid) return View(vm);
        var s = await _svc.GetStageByIdAsync(id);
        if (s == null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        var isSuper = user != null && await _userManager.IsInRoleAsync(user, "SuperAdmin");
        if (vm.ImageFile != null)
            s.ImageUrl = await UploadStageImage(vm.ImageFile) ?? s.ImageUrl;
        s.RegionId = vm.RegionId;
        s.Name = vm.Name;
        s.Introduction = vm.Introduction;
        s.SortOrder = vm.SortOrder;
        if (!isSuper)
        {
            s.AuditStatus = 0;
            s.SubmitterId = user?.Id;
        }
        await _svc.UpdateStageAsync(s);
        TempData["SuccessMessage"] = "已保存。";
        return RedirectToAction(nameof(Index), new { regionId = vm.RegionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var s = await _svc.GetStageByIdAsync(id);
        if (s == null) return NotFound();
        var rid = s.RegionId;
        if (!string.IsNullOrEmpty(s.ImageUrl) && s.ImageUrl.StartsWith("/images/opera-stages/", StringComparison.Ordinal))
        {
            var path = Path.Combine(_env.WebRootPath, s.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        await _svc.DeleteStageAsync(id);
        TempData["SuccessMessage"] = "已删除。";
        return RedirectToAction(nameof(Index), new { regionId = rid });
    }

    async Task<string?> UploadStageImage(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;
        var dir = Path.Combine(_env.WebRootPath, "images", "opera-stages");
        Directory.CreateDirectory(dir);
        var name = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
        var full = Path.Combine(dir, name);
        await using (var fs = new FileStream(full, FileMode.Create))
            await file.CopyToAsync(fs);
        return $"/images/opera-stages/{name}";
    }
}
