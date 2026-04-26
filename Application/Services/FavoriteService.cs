namespace OperaLearningSystem.Application.Services;

using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;

public class FavoriteService : IFavoriteService
{
    private readonly OperaDbContext _db;
    public FavoriteService(OperaDbContext db) => _db = db;

    public async Task<IEnumerable<Favorite>> GetUserFavoritesAsync(int userId)
        => await _db.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Play)
            .Include(f => f.Course)
            .Include(f => f.Master)
            .ToListAsync();

    public async Task AddFavoriteAsync(Favorite favorite)
    {
        _db.Favorites.Add(favorite);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveFavoriteAsync(int userId, int? entityId, string entityType)
    {
        Favorite favorite = entityType.ToLower() switch
        {
            "play" => await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.PlayId == entityId),
            "course" => await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.CourseId == entityId),
            "master" => await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.MasterId == entityId),
            _ => null
        };

        if (favorite != null)
        {
            _db.Favorites.Remove(favorite);
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsFavoriteAsync(int userId, int entityId, string entityType) => entityType.ToLower() switch
    {
        "play" => await _db.Favorites.AnyAsync(f => f.UserId == userId && f.PlayId == entityId),
        "course" => await _db.Favorites.AnyAsync(f => f.UserId == userId && f.CourseId == entityId),
        "master" => await _db.Favorites.AnyAsync(f => f.UserId == userId && f.MasterId == entityId),
        _ => false
    };
}