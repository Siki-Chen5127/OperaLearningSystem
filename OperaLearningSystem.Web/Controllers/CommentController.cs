using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Web.Helpers;

namespace OperaLearningSystem.Web.Controllers
{
    [Authorize]
    public class CommentController : Controller
    {
        private readonly OperaDbContext _context;
        private readonly UserManager<User> _userManager;

        public CommentController(OperaDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            string content,
            int? playId,
            int? courseId,
            int? postId,
            int? parentCommentId)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "评论内容不能为空。" });
            }

            var userId = int.Parse(_userManager.GetUserId(User));

            var comment = new Comment
            {
                Content = content.Trim(),
                UserId = userId,
                CreatedAt = DateTime.Now,
            };

            if (parentCommentId is int pid)
            {
                var parent = await _context.Comments.AsNoTracking().FirstOrDefaultAsync(c => c.Id == pid);
                if (parent == null)
                {
                    return Json(new { success = false, message = "回复的评论不存在。" });
                }

                if (courseId.HasValue && parent.CourseId != courseId)
                    return Json(new { success = false, message = "评论上下文不匹配。" });
                if (playId.HasValue && parent.PlayId != playId)
                    return Json(new { success = false, message = "评论上下文不匹配。" });
                if (postId.HasValue && parent.PostId != postId)
                    return Json(new { success = false, message = "评论上下文不匹配。" });

                comment.ParentCommentId = pid;
                comment.CourseId = parent.CourseId;
                comment.PlayId = parent.PlayId;
                comment.PostId = parent.PostId;
            }
            else
            {
                var n = (courseId.HasValue ? 1 : 0) + (playId.HasValue ? 1 : 0) + (postId.HasValue ? 1 : 0);
                if (n != 1)
                {
                    return Json(new { success = false, message = "评论目标无效。" });
                }

                comment.PlayId = playId;
                comment.CourseId = courseId;
                comment.PostId = postId;
            }

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var newComment = await _context.Comments
                .Include(c => c.User)
                .FirstAsync(c => c.Id == comment.Id);

            ViewData["AllComments"] = new List<Comment> { newComment };
            ViewData["CommentVoteStats"] =
                await CommentVoteStatsHelper.LoadAsync(_context, new[] { newComment.Id }, userId);
            ViewData["CommentCourseId"] = newComment.CourseId;
            ViewData["CommentPlayId"] = newComment.PlayId;
            ViewData["CommentPostId"] = newComment.PostId;

            return PartialView("_CommentPartial", newComment);
        }

        /// <summary>
        /// B 站式：再点同一态度取消；从赞切到踩、踩切到赞直接改值。
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Vote(int commentId, short value) =>
            CourseQuizSchemaPatcher.ExecuteWithSchemaRepairAsync(_context, () => VoteCore(commentId, value));

        private async Task<IActionResult> VoteCore(int commentId, short value)
        {
            if (value != 1 && value != -1)
                return Json(new { success = false, message = "参数无效。" });

            var userId = int.Parse(_userManager.GetUserId(User));

            var commentExists = await _context.Comments.AsNoTracking().AnyAsync(c => c.Id == commentId);
            if (!commentExists)
                return Json(new { success = false, message = "评论不存在。" });

            var vote = await _context.CommentVotes.FirstOrDefaultAsync(v =>
                v.UserId == userId && v.CommentId == commentId);

            if (vote == null)
            {
                _context.CommentVotes.Add(new CommentVote
                {
                    UserId = userId,
                    CommentId = commentId,
                    Value = value,
                });
            }
            else if (vote.Value == value)
            {
                _context.CommentVotes.Remove(vote);
            }
            else
            {
                vote.Value = value;
            }

            await _context.SaveChangesAsync();

            var up = await _context.CommentVotes.CountAsync(v => v.CommentId == commentId && v.Value == 1);
            var down = await _context.CommentVotes.CountAsync(v => v.CommentId == commentId && v.Value == -1);

            var userVote = await _context.CommentVotes.AsNoTracking()
                .Where(v => v.UserId == userId && v.CommentId == commentId)
                .Select(v => (short?)v.Value)
                .FirstOrDefaultAsync();

            return Json(new
            {
                success = true,
                commentId,
                up,
                down,
                userVote = (int)(userVote ?? 0),
            });
        }
    }
}
