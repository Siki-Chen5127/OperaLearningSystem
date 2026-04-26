namespace OperaLearningSystem.Core.Interfaces;

using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;

public interface ICommunityPostService
{
    Task<CommunityPost> GetByIdAsync(int id);
    Task AddAsync(CommunityPost post);
    Task UpdateAsync(CommunityPost post);
    Task DeleteAsync(int id);
    Task<PagedResult<CommunityPost>> GetPagedAsync(int pageNumber, int pageSize, string searchString, int? categoryId);
}