using Microsoft.AspNetCore.Mvc;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Web.Areas.Admin.ViewModels;

namespace OperaLearningSystem.Web.Areas.Admin.Controllers;

public class OperaStageRegionController : BaseAdminController
{
    private readonly IOperaStageService _svc;

    public OperaStageRegionController(IOperaStageService svc) => _svc = svc;

    public async Task<IActionResult> Index()
    {
        var list = (await _svc.GetAllRegionsAsync()).OrderBy(r => r.SortOrder).ThenBy(r => r.Id).ToList();
        return View(list);
    }

    public IActionResult Create() => View(new OperaStageRegionViewModel { SortOrder = 0 });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OperaStageRegionViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await _svc.AddRegionAsync(new OperaStageRegion { Name = vm.Name, SortOrder = vm.SortOrder });
        TempData["SuccessMessage"] = $"区域 “{vm.Name}” 已创建。";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var r = await _svc.GetRegionByIdAsync(id);
        if (r == null) return NotFound();
        return View(new OperaStageRegionViewModel { Id = r.Id, Name = r.Name, SortOrder = r.SortOrder });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OperaStageRegionViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        if (!ModelState.IsValid) return View(vm);
        var r = await _svc.GetRegionByIdAsync(id);
        if (r == null) return NotFound();
        r.Name = vm.Name;
        r.SortOrder = vm.SortOrder;
        await _svc.UpdateRegionAsync(r);
        TempData["SuccessMessage"] = "已保存。";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await _svc.GetRegionByIdAsync(id);
        if (r == null) return NotFound();
        if (r.Stages.Count > 0)
        {
            TempData["ErrorMessage"] = "请先删除该区域下的戏台条目。";
            return RedirectToAction(nameof(Index));
        }
        await _svc.DeleteRegionAsync(id);
        TempData["SuccessMessage"] = "区域已删除。";
        return RedirectToAction(nameof(Index));
    }
}
