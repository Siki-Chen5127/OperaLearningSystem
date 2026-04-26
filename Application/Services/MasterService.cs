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

public class MasterService : IMasterService
{
    private readonly OperaDbContext _db;
    private readonly IMemoryCache _cache;
    private const string AllMastersCacheKey = "AllMasters";
    private const string AllCategoriesCacheKey = "AllCategories";

    public MasterService(OperaDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }
    public async Task<IEnumerable<Master>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllMastersCacheKey, out IEnumerable<Master> masters))
        {
            return masters;
        }

        masters = await _db.Masters
                           .AsNoTracking()
                           .Include(m => m.Category)
                           .ToListAsync();

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));
        _cache.Set(AllMastersCacheKey, masters, cacheEntryOptions);

        return masters;
    }
    public async Task AddAsync(Master master)
    {
        _db.Masters.Add(master);
        await _db.SaveChangesAsync();
        _cache.Remove(AllMastersCacheKey);
        _cache.Remove(AllCategoriesCacheKey);
    }
    public async Task UpdateAsync(Master master)
    {
        _db.Masters.Update(master);
        await _db.SaveChangesAsync();
        _cache.Remove(AllMastersCacheKey);
        _cache.Remove(AllCategoriesCacheKey);
    }
    public async Task DeleteAsync(int id)
    {
        var master = await _db.Masters.FindAsync(id);
        if (master != null)
        {
            _db.Masters.Remove(master);
            await _db.SaveChangesAsync();
            _cache.Remove(AllMastersCacheKey);
            _cache.Remove(AllCategoriesCacheKey);
        }
    }
    public async Task<Master> GetByIdAsync(int id)
    {
        return await _db.Masters
            .Include(m => m.Category)
            .Include(m => m.PlayMasters) // 顺便加载关联信息，以备详情页使用
            .Include(m => m.Likes)
            .Include(m => m.Favorites)
            .FirstOrDefaultAsync(m => m.Id == id);
    }
    public async Task<IEnumerable<Play>> GetPlaysByMasterAsync(int masterId)
    {
        return await _db.PlayMasters
            .Where(pm => pm.MasterId == masterId)
            .Select(pm => pm.Play)
            .Include(p => p.Category)
            .ToListAsync();
    }
    public async Task<PagedResult<Master>> GetPagedAsync(int pageNumber, int pageSize, string searchString, int? categoryId, bool onlyApproved = false)
    {

        // 1. 修改查询源为 _db.Masters，前台已看审核过的
        var query = _db.Masters
                        .Include(m => m.Category)
                        .AsQueryable();
        if (onlyApproved)
        {
            query = query.Where(m => m.AuditStatus == 1);
        }

        // 2. 筛选逻辑：按剧种
        if (categoryId.HasValue && categoryId > 0)
        {
            query = query.Where(m => m.CategoryId == categoryId.Value);
        }

        // 3. 筛选逻辑：按名字
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(m => m.Name.Contains(searchString));
        }

        // 4. 分页逻辑
        var totalItems = await query.CountAsync();
        var items = await query.OrderByDescending(m => m.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 5. 返回 Master 类型的分页结果
        return new PagedResult<Master> { Items = items, TotalItems = totalItems, PageNumber = pageNumber, PageSize = pageSize };
    }
    public async Task<int> GetMasterCountAsync()
    {
        return await _db.Masters.CountAsync();
    }
}