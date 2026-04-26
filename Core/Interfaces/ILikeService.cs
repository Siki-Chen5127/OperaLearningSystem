namespace OperaLearningSystem.Core.Interfaces;

using OperaLearningSystem.Core.Entities;

public interface ILikeService
{
    Task AddLikeAsync(Like like);
    Task RemoveLikeAsync(int userId, int entityId, string entityType);
    Task<bool> IsLikedAsync(int userId, int entityId, string entityType);
    Task<int> GetLikesCountAsync(int entityId, string entityType);
}