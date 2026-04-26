namespace OperaLearningSystem.Core.Interfaces;

using OperaLearningSystem.Core.Entities;

public interface IFavoriteService
{
    Task<IEnumerable<Favorite>> GetUserFavoritesAsync(int userId);
    Task AddFavoriteAsync(Favorite favorite);
    Task RemoveFavoriteAsync(int userId, int? entityId, string entityType);
    Task<bool> IsFavoriteAsync(int userId, int entityId, string entityType);
}