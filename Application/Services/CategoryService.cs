namespace OperaLearningSystem.Application.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class CategoryService : ICategoryService
{
    private readonly OperaDbContext _db;
    private readonly IMemoryCache _cache;
    private const string AllCategoriesCacheKey = "AllCategories";

    public CategoryService(OperaDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PagedResult<Category>> GetPagedAsync(int pageNumber, int pageSize, string searchString, bool onlyApproved = false)
    {
        var query = _db.Categories
               .AsNoTracking()
               .Include(c => c.Plays)
               .Include(c => c.Courses)
               .Include(c => c.Masters)
               .AsQueryable();

        if (onlyApproved)
        {
            query = query.Where(c => c.AuditStatus == 1);
        }

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(c => c.Name.Contains(searchString) || c.Description.Contains(searchString));
        }

        var totalItems = await query.CountAsync();
        var items = await query.OrderBy(c => c.Id)
                               .Skip((pageNumber - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync();

        return new PagedResult<Category> { Items = items, TotalItems = totalItems, PageNumber = pageNumber, PageSize = pageSize };
    }

    public async Task<Category> GetCategoryDetailsByIdAsync(int id)
    {
        return await _db.Categories
            .AsNoTracking()
            .Include(c => c.Plays)
            .Include(c => c.Masters)
            .Include(c => c.Courses)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllCategoriesCacheKey, out IEnumerable<Category> categories)) { return categories; }
        categories = await _db.Categories.AsNoTracking().Include(c => c.Plays).Include(c => c.Courses).Include(c => c.Masters).ToListAsync();
        var cacheEntryOptions = new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(5));
        _cache.Set(AllCategoriesCacheKey, categories, cacheEntryOptions);
        return categories;
    }
    public async Task<Category> GetByIdAsync(int id) => await _db.Categories.FindAsync(id);
    public async Task AddAsync(Category category)
    { 
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(); 
        _cache.Remove(AllCategoriesCacheKey);
    }
    public async Task UpdateAsync(Category category) { _db.Categories.Update(category); await _db.SaveChangesAsync(); _cache.Remove(AllCategoriesCacheKey); }
    public async Task DeleteAsync(int id) { var category = await _db.Categories.FindAsync(id); if (category != null) { _db.Categories.Remove(category); await _db.SaveChangesAsync(); _cache.Remove(AllCategoriesCacheKey); } }
    public async Task<IEnumerable<CategorySimpleDto>> GetCategoriesForSelectListAsync()
    {
        return await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategorySimpleDto { Id = c.Id, Name = c.Name })
            .ToListAsync();
    }
}