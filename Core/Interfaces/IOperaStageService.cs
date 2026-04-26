using OperaLearningSystem.Core.Entities;
using System.Threading;

namespace OperaLearningSystem.Core.Interfaces;

public interface IOperaStageService
{
    Task<IReadOnlyList<OperaStageRegion>> GetPublishedRegionsWithStagesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<OperaStageRegion>> GetAllRegionsAsync(CancellationToken ct = default);
    Task<OperaStageRegion?> GetRegionByIdAsync(int id, CancellationToken ct = default);
    Task AddRegionAsync(OperaStageRegion region, CancellationToken ct = default);
    Task UpdateRegionAsync(OperaStageRegion region, CancellationToken ct = default);
    Task DeleteRegionAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<OperaStage>> GetStagesByRegionAsync(int regionId, bool includeAllAudit = false, CancellationToken ct = default);
    Task<OperaStage?> GetStageByIdAsync(int id, CancellationToken ct = default);
    Task AddStageAsync(OperaStage stage, CancellationToken ct = default);
    Task UpdateStageAsync(OperaStage stage, CancellationToken ct = default);
    Task DeleteStageAsync(int id, CancellationToken ct = default);
}
