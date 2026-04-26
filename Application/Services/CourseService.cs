namespace OperaLearningSystem.Application.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;

public class CourseService : ICourseService
{
    private readonly OperaDbContext _db;
    private readonly IMemoryCache _cache; // 1. 添加缓存字段
    private const string AllCoursesCacheKey = "AllCourses"; // 定义缓存键
    private const string AllCategoriesCacheKey = "AllCategories"; // 引用剧种的缓存键
    public CourseService(OperaDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }
    public async Task<IEnumerable<Course>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllCoursesCacheKey, out IEnumerable<Course> courses))
        {
            return courses;
        }
        courses = await _db.Courses
                           .AsNoTracking()
                           .Include(c => c.Category)
                           .ToListAsync();

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));
        _cache.Set(AllCoursesCacheKey, courses, cacheEntryOptions);

        return courses;
    }
    public async Task<Course> GetCourseDetailsByIdAsync(int id)
    {
        return await _db.Courses
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Likes)
            .Include(c => c.Favorites)
            .Include(c => c.Comments)
                .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id);
    }
    public async Task AddAsync(Course course)
    {
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();
        _cache.Remove(AllCoursesCacheKey);
        _cache.Remove(AllCategoriesCacheKey);
    }
    public async Task UpdateAsync(Course course)
    {
        var existing = await _db.Courses.FirstOrDefaultAsync(c => c.Id == course.Id);
        if (existing == null)
        {
            return;
        }

        existing.Name = course.Name;
        existing.Description = course.Description;
        existing.VideoUrl = course.VideoUrl;
        existing.BilibiliEmbedHtml = course.BilibiliEmbedHtml;
        existing.CategoryId = course.CategoryId;
        existing.IsFeatured = course.IsFeatured;
        existing.ImageUrl = course.ImageUrl;
        existing.SubmitterId = course.SubmitterId;
        existing.AuditStatus = course.AuditStatus;

        await _db.SaveChangesAsync();
        _cache.Remove(AllCoursesCacheKey);
        _cache.Remove(AllCategoriesCacheKey);
    }
    public async Task DeleteAsync(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course != null)
        {
            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();
            _cache.Remove(AllCoursesCacheKey);
            _cache.Remove(AllCategoriesCacheKey);
        }
    }
    public async Task<IEnumerable<Course>> GetFeaturedCoursesAsync()
        => await _db.Courses
            .Where(c => c.IsFeatured)
            .Include(c => c.Category)
            .ToListAsync();

    public async Task<List<Course>> GetRandomSpotlightCoursesAsync(int count, bool onlyApproved = true)
    {
        var query = _db.Courses.AsNoTracking().Include(c => c.Category).AsQueryable();
        if (onlyApproved)
            query = query.Where(c => c.AuditStatus == 1);

        var featured = await query.Where(c => c.IsFeatured).ToListAsync();
        var rnd = new Random();
        var picked = featured.OrderBy(_ => rnd.Next()).Take(count).ToList();

        if (picked.Count < count)
        {
            var pickedIds = picked.Select(c => c.Id).ToHashSet();
            var rest = await query.Where(c => !pickedIds.Contains(c.Id)).ToListAsync();
            foreach (var c in rest.OrderBy(_ => rnd.Next()))
            {
                if (picked.Count >= count) break;
                picked.Add(c);
            }
        }

        return picked;
    }

    public async Task<PagedResult<Course>> GetPagedAsync(int pageNumber, int pageSize, string searchString, int? categoryId, bool onlyApproved = false)
    {
        var query = _db.Courses.Include(c => c.Category).AsQueryable();

        if (onlyApproved)
        {
            query = query.Where(c => c.AuditStatus == 1);
        }
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(c => c.Name.Contains(searchString) || c.Description.Contains(searchString));
        }

        if (categoryId.HasValue && categoryId > 0)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }

        var totalItems = await query.CountAsync();
        var items = await query.OrderByDescending(c => c.Id)
                               .Skip((pageNumber - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return new PagedResult<Course> { Items = items, TotalItems = totalItems, PageNumber = pageNumber, PageSize = pageSize };
    }
    public async Task<int> GetCourseCountAsync()
    {
        return await _db.Courses.CountAsync();
    }
    public async Task<IEnumerable<Course>> GetRecentCoursesAsync(int count)
    {
        return await _db.Courses
            .Include(c => c.Category) // 包含分类信息
            .OrderByDescending(c => c.Id)
            .Take(count)
            .ToListAsync();
    }
}