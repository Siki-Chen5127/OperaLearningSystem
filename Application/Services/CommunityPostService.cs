namespace OperaLearningSystem.Application.Services;

using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;

public class CommunityPostService : ICommunityPostService
{
    private readonly OperaDbContext _db;
    public CommunityPostService(OperaDbContext db) => _db = db;
    public async Task<PagedResult<CommunityPost>> GetPagedAsync(int pageNumber, int pageSize, string searchString, int? categoryId)
    {
        var query = _db.CommunityPosts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .AsQueryable();
        if (categoryId.HasValue && categoryId > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }
        if (!string.IsNullOrEmpty(searchString))
        {
            query = query.Where(p => p.Title.Contains(searchString) || p.Author.UserName.Contains(searchString));
        }
        var totalItems = await query.CountAsync();
        var items = await query.OrderByDescending(p => p.CreatedTime)
                                 .Skip((pageNumber - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync();

        return new PagedResult<CommunityPost> { Items = items, TotalItems = totalItems, PageNumber = pageNumber, PageSize = pageSize };
    }
    public async Task<CommunityPost> GetByIdAsync(int id) 
        => await _db.CommunityPosts.Include(p => p.Author).Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
    public async Task AddAsync(CommunityPost post) 
    { 
        _db.CommunityPosts.Add(post); await _db.SaveChangesAsync();
    }
    public async Task UpdateAsync(CommunityPost post) 
    { 
        _db.CommunityPosts.Update(post); await _db.SaveChangesAsync(); 
    }
    public async Task DeleteAsync(int id) 
    { 
        var post = await _db.CommunityPosts.FindAsync(id); 
        if (post != null) { 
            _db.CommunityPosts.Remove(post);
            await _db.SaveChangesAsync(); 
        } 
    }
}