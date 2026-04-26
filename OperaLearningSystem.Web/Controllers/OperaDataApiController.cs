using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;

namespace OperaLearningSystem.Web.Controllers;

[Route("api/opera")]
[ApiController]
public class OperaDataApiController : ControllerBase
{
    private readonly OperaDbContext _db;
    private readonly IOperaStageService _operaStageService;

    public OperaDataApiController(OperaDbContext db, IOperaStageService operaStageService)
    {
        _db = db;
        _operaStageService = operaStageService;
    }

    [HttpGet("stage-regions")]
    public async Task<IActionResult> StageRegions(CancellationToken ct)
    {
        var regions = await _operaStageService.GetPublishedRegionsWithStagesAsync(ct);
        var payload = regions.Select(r => new
        {
            r.Id,
            r.Name,
            r.SortOrder,
            Stages = r.Stages.Select(s => new
            {
                s.Id,
                s.Name,
                Introduction = s.Introduction ?? "",
                ImageUrl = string.IsNullOrEmpty(s.ImageUrl) ? "/images/default.png" : s.ImageUrl
            }).ToList()
        }).ToList();
        return Ok(payload);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories(CancellationToken ct)
    {
        var rows = await _db.Categories.AsNoTracking()
            .Where(c => c.AuditStatus == 1)
            .OrderBy(c => c.Id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                Description = c.Description ?? "",
                ImageUrl = string.IsNullOrEmpty(c.ImageUrl) ? "/images/default.png" : c.ImageUrl
            })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("categories/{id:int}")]
    public async Task<IActionResult> CategoryDetail(int id, CancellationToken ct)
    {
        var c = await _db.Categories.AsNoTracking()
            .Where(x => x.Id == id && x.AuditStatus == 1)
            .Select(x => new
            {
                x.Id, x.Name,
                Description = x.Description ?? "",
                History = x.History ?? "",
                ImageUrl = string.IsNullOrEmpty(x.ImageUrl) ? "/images/default.png" : x.ImageUrl,
                PlayCount = _db.Plays.Count(p => p.CategoryId == x.Id && p.AuditStatus == 1),
                MasterCount = _db.Masters.Count(m => m.CategoryId == x.Id && m.AuditStatus == 1),
                CourseCount = _db.Courses.Count(co => co.CategoryId == x.Id && co.AuditStatus == 1)
            })
            .FirstOrDefaultAsync(ct);
        if (c == null) return NotFound();
        return Ok(c);
    }

    [HttpGet("categories/{id:int}/plays")]
    public async Task<IActionResult> PlaysByCategory(int id, CancellationToken ct)
    {
        var rows = await _db.Plays.AsNoTracking()
            .Where(p => p.CategoryId == id && p.AuditStatus == 1)
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                p.Id,
                Title = p.Title,
                Synopsis = p.Synopsis ?? "",
                ImageUrl = string.IsNullOrEmpty(p.ImageUrl) ? "/images/default.png" : p.ImageUrl
            })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("masters")]
    public async Task<IActionResult> AllMasters([FromQuery] string? q, CancellationToken ct)
    {
        var query = _db.Masters.AsNoTracking().Where(m => m.AuditStatus == 1);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(m => m.Name.Contains(q));
        var rows = await query.OrderBy(m => m.Id)
            .Select(m => new
            {
                m.Id, m.Name,
                Introduction = m.Introduction ?? "",
                ImageUrl = string.IsNullOrEmpty(m.ImageUrl) ? "/images/default.png" : m.ImageUrl,
                CategoryName = m.Category != null ? m.Category.Name : ""
            })
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpGet("masters/{id:int}")]
    public async Task<IActionResult> MasterDetail(int id, CancellationToken ct)
    {
        var m = await _db.Masters.AsNoTracking()
            .Where(x => x.Id == id && x.AuditStatus == 1)
            .Select(x => new
            {
                x.Id, x.Name,
                Introduction = x.Introduction ?? "",
                ImageUrl = string.IsNullOrEmpty(x.ImageUrl) ? "/images/default.png" : x.ImageUrl,
                CategoryName = x.Category != null ? x.Category.Name : ""
            })
            .FirstOrDefaultAsync(ct);
        if (m == null) return NotFound();
        return Ok(m);
    }

    [HttpGet("plays/{id:int}/masters")]
    public async Task<IActionResult> MastersByPlay(int id, CancellationToken ct)
    {
        var masterIds = await _db.PlayMasters.AsNoTracking()
            .Where(pm => pm.PlayId == id)
            .Select(pm => pm.MasterId)
            .ToListAsync(ct);

        var rows = await _db.Masters.AsNoTracking()
            .Where(m => masterIds.Contains(m.Id) && m.AuditStatus == 1)
            .OrderBy(m => m.Id)
            .Select(m => new
            {
                m.Id,
                m.Name,
                Introduction = m.Introduction ?? "",
                ImageUrl = string.IsNullOrEmpty(m.ImageUrl) ? "/images/default.png" : m.ImageUrl
            })
            .ToListAsync(ct);
        return Ok(rows);
    }
}
