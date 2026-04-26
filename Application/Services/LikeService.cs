namespace OperaLearningSystem.Application.Services;

using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;

public class LikeService : ILikeService
{
    private readonly OperaDbContext _db;
    public LikeService(OperaDbContext db) => _db = db;

    public async Task AddLikeAsync(Like like)
    {
        _db.Likes.Add(like);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveLikeAsync(int userId, int entityId, string entityType)
    {
        Like like = entityType.ToLower() switch
        {
            "play" => await _db.Likes.FirstOrDefaultAsync(l => l.UserId == userId && l.PlayId == entityId),
            "course" => await _db.Likes.FirstOrDefaultAsync(l => l.UserId == userId && l.CourseId == entityId),
            "master" => await _db.Likes.FirstOrDefaultAsync(l => l.UserId == userId && l.MasterId == entityId),
            "comment" => await _db.Likes.FirstOrDefaultAsync(l => l.UserId == userId && l.CommentId == entityId),
            _ => null
        };

        if (like != null)
        {
            _db.Likes.Remove(like);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsLikedAsync(int userId, int entityId, string entityType) => entityType.ToLower() switch
    {
        "play" => await _db.Likes.AnyAsync(l => l.UserId == userId && l.PlayId == entityId),
        "course" => await _db.Likes.AnyAsync(l => l.UserId == userId && l.CourseId == entityId),
        "master" => await _db.Likes.AnyAsync(l => l.UserId == userId && l.MasterId == entityId),
        "comment" => await _db.Likes.AnyAsync(l => l.UserId == userId && l.CommentId == entityId),
        _ => false
    };

    public async Task<int> GetLikesCountAsync(int entityId, string entityType) => entityType.ToLower() switch
    {
        "play" => await _db.Likes.CountAsync(l => l.PlayId == entityId),
        "course" => await _db.Likes.CountAsync(l => l.CourseId == entityId),
        "master" => await _db.Likes.CountAsync(l => l.MasterId == entityId),
        "comment" => await _db.Likes.CountAsync(l => l.CommentId == entityId),
        _ => 0
    };
}