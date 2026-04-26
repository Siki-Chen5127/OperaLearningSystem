namespace OperaLearningSystem.Core.Interfaces;

using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;

public interface ICourseService
{
    Task<IEnumerable<Course>> GetAllAsync();
    Task<Course> GetCourseDetailsByIdAsync(int id);
    Task AddAsync(Course course);
    Task UpdateAsync(Course course);
    Task DeleteAsync(int id);
    Task<IEnumerable<Course>> GetFeaturedCoursesAsync();
    Task<PagedResult<Course>> GetPagedAsync(int pageNumber, int pageSize, string searchString, int? categoryId, bool onlyApproved = false);
    Task<int> GetCourseCountAsync();
    /// <summary>优先随机精读课程，不足则用其余已审课程补足，用于首页传习私塾 spotlight。</summary>
    Task<List<Course>> GetRandomSpotlightCoursesAsync(int count, bool onlyApproved = true);
    Task<IEnumerable<Course>> GetRecentCoursesAsync(int count);
}