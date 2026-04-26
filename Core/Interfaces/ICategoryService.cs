namespace OperaLearningSystem.Core.Interfaces;

using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;

public interface ICategoryService
{
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category> GetByIdAsync(int id); 
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(int id);
    Task<IEnumerable<CategorySimpleDto>> GetCategoriesForSelectListAsync();
    Task<PagedResult<Category>> GetPagedAsync(int pageNumber, int pageSize, string searchString, bool onlyApproved = false);
    Task<Category> GetCategoryDetailsByIdAsync(int id);
}