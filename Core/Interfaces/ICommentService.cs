namespace OperaLearningSystem.Core.Interfaces;

using OperaLearningSystem.Core.Entities;

public interface ICommentService
{
    Task<IEnumerable<Comment>> GetByPlayIdAsync(int playId);
    Task<IEnumerable<Comment>> GetByCourseIdAsync(int courseId);
    Task<IEnumerable<Comment>> GetByPostIdAsync(int postId);
    Task AddAsync(Comment comment);
    Task DeleteAsync(int id);
}