namespace OperaLearningSystem.Core.Interfaces;

using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;

public interface IMasterService
{
    Task<Master> GetByIdAsync(int id);
    Task AddAsync(Master master);
    Task UpdateAsync(Master master);
    Task DeleteAsync(int id);
    Task<IEnumerable<Play>> GetPlaysByMasterAsync(int masterId);
    Task<PagedResult<Master>> GetPagedAsync(int pageNumber, int pageSize, string searchString, int? categoryId, bool onlyApproved = false); Task<int> GetMasterCountAsync();
    Task<IEnumerable<Master>> GetAllAsync();
}