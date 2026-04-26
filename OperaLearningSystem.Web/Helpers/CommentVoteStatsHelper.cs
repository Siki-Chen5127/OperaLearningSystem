using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Infrastructure.Data;

namespace OperaLearningSystem.Web.Helpers;

public sealed class CommentVoteStatsDto
{
    public int Up { get; set; }
    public int Down { get; set; }
    public int UserVote { get; set; }
}

public static class CommentVoteStatsHelper
{
    public static async Task<Dictionary<int, CommentVoteStatsDto>> LoadAsync(
        OperaDbContext db,
        IEnumerable<int> commentIds,
        int? currentUserId,
        CancellationToken ct = default)
    {
        var ids = commentIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, CommentVoteStatsDto>();

        return await CourseQuizSchemaPatcher.ExecuteWithSchemaRepairAsync(db, async () =>
        {
            var votes = await db.CommentVotes.AsNoTracking()
                .Where(v => ids.Contains(v.CommentId))
                .Select(v => new { v.CommentId, v.UserId, v.Value })
                .ToListAsync(ct);

            var dict = ids.ToDictionary(id => id, _ => new CommentVoteStatsDto());
            foreach (var v in votes)
            {
                var s = dict[v.CommentId];
                if (v.Value == 1) s.Up++;
                else if (v.Value == -1) s.Down++;
                if (currentUserId.HasValue && v.UserId == currentUserId.Value)
                    s.UserVote = v.Value;
            }

            return dict;
        }, ct);
    }
}
