namespace OperaLearningSystem.Core.Interfaces;

using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;

public interface IPlayService
{
    Task<IEnumerable<Play>> GetAllAsync();
    Task<Play> GetByIdAsync(int id);
    Task<Play> GetByIdWithMastersAsync(int id);
    Task AddAsync(Play play);
    Task UpdateAsync(Play play);
    Task DeleteAsync(int id);
    Task AddMasterToPlayAsync(int playId, int masterId);
    Task RemoveMasterFromPlayAsync(int playId, int masterId);
    Task<PagedResult<Play>> GetPagedAsync(int pageNumber, int pageSize, string searchString, int? categoryId, bool onlyApproved = false); Task<int> GetPlayCountAsync();
    Task UpdatePlayMastersAsync(int playId, List<int> selectedMasterIds);
    Task<Play> GetPlayDetailsByIdAsync(int id);
    Task<List<Play>> GetRecommendedAsync(int count);
    Task<List<OperaLyric>> GetRandomLyricsAsync(int count);
}