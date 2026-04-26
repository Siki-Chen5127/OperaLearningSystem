namespace OperaLearningSystem.Application.Services;

using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;

public class CommentService : ICommentService
{
    private readonly OperaDbContext _db;
    public CommentService(OperaDbContext db) => _db = db;

    public async Task AddAsync(Comment comment)
    {
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var comment = await _db.Comments.FindAsync(id);
        if (comment != null)
        {
            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Comment>> GetByCourseIdAsync(int courseId)
        => await _db.Comments
            .Where(c => c.CourseId == courseId)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Comment>> GetByPlayIdAsync(int playId)
        => await _db.Comments
            .Where(c => c.PlayId == playId)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Comment>> GetByPostIdAsync(int postId)
        => await _db.Comments
            .Where(c => c.PostId == postId)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
}