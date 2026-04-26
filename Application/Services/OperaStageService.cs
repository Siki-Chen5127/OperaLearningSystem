using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;

namespace OperaLearningSystem.Application.Services;

public class OperaStageService : IOperaStageService
{
    private readonly OperaDbContext _db;

    public OperaStageService(OperaDbContext db) => _db = db;

    public async Task<IReadOnlyList<OperaStageRegion>> GetPublishedRegionsWithStagesAsync(CancellationToken ct = default)
    {
        var regions = await _db.OperaStageRegions.AsNoTracking()
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
            .Include(r => r.Stages.Where(s => s.AuditStatus == 1))
            .ToListAsync(ct);
        foreach (var r in regions)
            r.Stages = r.Stages.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToList();
        return regions;
    }

    public async Task<IReadOnlyList<OperaStageRegion>> GetAllRegionsAsync(CancellationToken ct = default)
    {
        return await _db.OperaStageRegions.AsNoTracking()
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
            .ToListAsync(ct);
    }

    public async Task<OperaStageRegion?> GetRegionByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.OperaStageRegions
            .Include(r => r.Stages)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task AddRegionAsync(OperaStageRegion region, CancellationToken ct = default)
    {
        _db.OperaStageRegions.Add(region);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateRegionAsync(OperaStageRegion region, CancellationToken ct = default)
    {
        _db.OperaStageRegions.Update(region);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteRegionAsync(int id, CancellationToken ct = default)
    {
        var r = await _db.OperaStageRegions.Include(x => x.Stages).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r == null) return;
        _db.OperaStages.RemoveRange(r.Stages);
        _db.OperaStageRegions.Remove(r);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OperaStage>> GetStagesByRegionAsync(int regionId, bool includeAllAudit = false, CancellationToken ct = default)
    {
        var q = _db.OperaStages.AsNoTracking().Where(s => s.RegionId == regionId);
        if (!includeAllAudit)
            q = q.Where(s => s.AuditStatus == 1);
        return await q.OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync(ct);
    }

    public async Task<OperaStage?> GetStageByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.OperaStages.Include(s => s.Region).FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task AddStageAsync(OperaStage stage, CancellationToken ct = default)
    {
        _db.OperaStages.Add(stage);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateStageAsync(OperaStage stage, CancellationToken ct = default)
    {
        _db.OperaStages.Update(stage);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteStageAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.OperaStages.FindAsync(new object[] { id }, ct);
        if (s == null) return;
        _db.OperaStages.Remove(s);
        await _db.SaveChangesAsync(ct);
    }
}
