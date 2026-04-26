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

public class PlayService : IPlayService
{
    private readonly OperaDbContext _db;
    private readonly IMemoryCache _cache;
    private const string AllPlaysCacheKey = "AllPlays";
    private const string AllCategoriesCacheKey = "AllCategories";

    public PlayService(OperaDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }
    public async Task<IEnumerable<Play>> GetAllAsync()
    {
        if (_cache.TryGetValue(AllPlaysCacheKey, out IEnumerable<Play> plays))
        {
            return plays;
        }

        plays = await _db.Plays
                         .AsNoTracking()
                         .Include(p => p.Category)
                         .Include(p => p.PlayMasters)
                         .ThenInclude(pm => pm.Master)
                         .ToListAsync();

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5));
        _cache.Set(AllPlaysCacheKey, plays, cacheEntryOptions);

        return plays;
    }
    public async Task<Play> GetByIdAsync(int id)
        => await _db.Plays
            .Include(p => p.Category)
            .Include(p => p.PlayMasters)
                .ThenInclude(pm => pm.Master)
            .FirstOrDefaultAsync(p => p.Id == id);
    public async Task AddAsync(Play play)
    {
        _db.Plays.Add(play);
        await _db.SaveChangesAsync();
        _cache.Remove(AllPlaysCacheKey);
        _cache.Remove(AllCategoriesCacheKey);
    }
    public async Task UpdateAsync(Play play)
    {
        _db.Plays.Update(play);
        await _db.SaveChangesAsync();
        _cache.Remove(AllPlaysCacheKey);
        _cache.Remove(AllCategoriesCacheKey);
    }
    public async Task DeleteAsync(int id)
    {
        var play = await _db.Plays.FindAsync(id);
        if (play != null)
        {
            _db.Plays.Remove(play);
            await _db.SaveChangesAsync();
            _cache.Remove(AllPlaysCacheKey);
            _cache.Remove(AllCategoriesCacheKey);
        }
    }
    public async Task AddMasterToPlayAsync(int playId, int masterId)
    {
        var exists = await _db.PlayMasters.AnyAsync(pm => pm.PlayId == playId && pm.MasterId == masterId);
        if (!exists)
        {
            _db.PlayMasters.Add(new PlayMaster { PlayId = playId, MasterId = masterId });
            await _db.SaveChangesAsync();
        }
    }
    public async Task RemoveMasterFromPlayAsync(int playId, int masterId)
    {
        var playMaster = await _db.PlayMasters.FindAsync(playId, masterId);
        if (playMaster != null)
        {
            _db.PlayMasters.Remove(playMaster);
            await _db.SaveChangesAsync();
        }
    }
    public async Task<PagedResult<Play>> GetPagedAsync(int pageNumber, int pageSize, string searchString, int? categoryId, bool onlyApproved = false)
    {
        var query = _db.Plays
                        .Include(p => p.Category)
                        .Include(p => p.PlayMasters)
                        .ThenInclude(pm => pm.Master)
                        .AsQueryable();
        if (onlyApproved)
        {
            query = query.Where(p => p.AuditStatus == 1);
        }
        if (categoryId.HasValue && categoryId > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(p => p.Title.Contains(searchString));
        }

        var totalItems = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return new PagedResult<Play> { Items = items, TotalItems = totalItems, PageNumber = pageNumber, PageSize = pageSize };
    }
    public async Task<int> GetPlayCountAsync()
    {
        return await _db.Plays.CountAsync();
    }
    public async Task<Play> GetByIdWithMastersAsync(int id)
    {
        return await _db.Plays
                        .Include(p => p.PlayMasters)
                        .FirstOrDefaultAsync(p => p.Id == id);
    }
    public async Task UpdatePlayMastersAsync(int playId, List<int> selectedMasterIds)
    {
        selectedMasterIds ??= new List<int>();

        var play = await _db.Plays
            .Include(p => p.PlayMasters)
            .FirstOrDefaultAsync(p => p.Id == playId);

        if (play == null) return;

        var existingMasterIds = play.PlayMasters.Select(pm => pm.MasterId).ToList();

        var idsToAdd = selectedMasterIds.Except(existingMasterIds).ToList();
        var idsToRemove = existingMasterIds.Except(selectedMasterIds).ToList();

        if (idsToRemove.Any())
        {
            var mastersToRemove = play.PlayMasters.Where(pm => idsToRemove.Contains(pm.MasterId)).ToList();
            _db.PlayMasters.RemoveRange(mastersToRemove);
        }

        if (idsToAdd.Any())
        {
            foreach (var masterId in idsToAdd)
            {
                _db.PlayMasters.Add(new PlayMaster { PlayId = playId, MasterId = masterId });
            }
        }

        if (idsToAdd.Any() || idsToRemove.Any())
        {
            await _db.SaveChangesAsync();
        }
    }
    public async Task<Play> GetPlayDetailsByIdAsync(int id)
    {
        return await _db.Plays
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.PlayMasters)
                .ThenInclude(pm => pm.Master)
            .Include(p => p.Likes)
            .Include(p => p.Favorites)
            .Include(p => p.Comments)
                .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.AuditStatus == 1);
    }
    public async Task<List<Play>> GetRecommendedAsync(int count)
    {
        return await _db.Plays
            .AsNoTracking()
            .Where(p => p.AuditStatus == 1)
            .Include(p => p.Category) // 包含分类以显示图片或名称
            .OrderBy(r => Guid.NewGuid())
            .Take(count)
            .ToListAsync();
    }
    public async Task<List<OperaLyric>> GetRandomLyricsAsync(int count)
    {
        // 随机获取 N 条戏词，并包含关联的剧目信息
        return await _db.OperaLyrics
            .AsNoTracking()
            .Include(l => l.Play)
            .OrderBy(r => Guid.NewGuid())
            .Take(count)
            .ToListAsync();
    }
}