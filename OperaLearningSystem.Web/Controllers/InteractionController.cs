using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;

namespace OperaLearningSystem.Web.Controllers
{
    [Authorize]
    public class InteractionController : Controller
    {
        private readonly OperaDbContext _context;
        private readonly UserManager<User> _userManager;

        public InteractionController(OperaDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        #region --- 点赞 (Like) Actions ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePlayLike(int playId)
        {
            var userId = int.Parse(_userManager.GetUserId(User));
            return await ToggleLikeAsync(userId, playId: playId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseLike(int courseId)
        {
            var userId = int.Parse(_userManager.GetUserId(User));
            return await ToggleLikeAsync(userId, courseId: courseId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMasterLike(int masterId)
        {
            var userId = int.Parse(_userManager.GetUserId(User));
            return await ToggleLikeAsync(userId, masterId: masterId);
        }
        #endregion

        #region --- 收藏 (Favorite) Actions ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePlayFavorite(int playId)
        {
            var userId = int.Parse(_userManager.GetUserId(User));
            return await ToggleFavoriteAsync(userId, playId: playId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseFavorite(int courseId)
        {
            var userId = int.Parse(_userManager.GetUserId(User));
            return await ToggleFavoriteAsync(userId, courseId: courseId);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleMasterFavorite(int masterId)
        {
            var userId = int.Parse(_userManager.GetUserId(User));
            return await ToggleFavoriteAsync(userId, masterId: masterId);
        }
        #endregion

        #region --- 私有辅助方法 (Private Helpers) ---
        /// <summary>
        /// 剧目/课程/名家点赞：必须按「单一外键 + 其余为 null」匹配。
        /// 不能用 (PlayId==playArg || CourseId==courseArg || MasterId==masterArg)，否则 null 会转成 SQL IS NULL，匹配到大量无关行。
        /// </summary>
        private async Task<JsonResult> ToggleLikeAsync(int userId, int? playId = null, int? courseId = null, int? masterId = null)
        {
            var contentLikes = _context.Likes.Where(l =>
                l.UserId == userId && l.CommunityPostId == null && l.CommentId == null);

            Like? existing = null;
            if (playId is int p)
                existing = await contentLikes.FirstOrDefaultAsync(l =>
                    l.PlayId == p && l.CourseId == null && l.MasterId == null);
            else if (courseId is int c)
                existing = await contentLikes.FirstOrDefaultAsync(l =>
                    l.CourseId == c && l.PlayId == null && l.MasterId == null);
            else if (masterId is int m)
                existing = await contentLikes.FirstOrDefaultAsync(l =>
                    l.MasterId == m && l.PlayId == null && l.CourseId == null);
            else
                return Json(new { success = false, message = "参数无效" });

            bool isNowLiked;
            if (existing != null)
            {
                _context.Likes.Remove(existing);
                isNowLiked = false;
            }
            else
            {
                _context.Likes.Add(new Like
                {
                    UserId = userId,
                    PlayId = playId,
                    CourseId = courseId,
                    MasterId = masterId,
                    ReactionKind = 0
                });
                isNowLiked = true;
            }

            await _context.SaveChangesAsync();

            int totalLikes;
            if (playId is int pCount)
                totalLikes = await _context.Likes.CountAsync(l =>
                    l.PlayId == pCount && l.CourseId == null && l.MasterId == null
                    && l.CommunityPostId == null && l.CommentId == null);
            else if (courseId is int cCount)
                totalLikes = await _context.Likes.CountAsync(l =>
                    l.CourseId == cCount && l.PlayId == null && l.MasterId == null
                    && l.CommunityPostId == null && l.CommentId == null);
            else if (masterId is int mCount)
                totalLikes = await _context.Likes.CountAsync(l =>
                    l.MasterId == mCount && l.PlayId == null && l.CourseId == null
                    && l.CommunityPostId == null && l.CommentId == null);
            else
                totalLikes = 0;

            return Json(new { success = true, isLiked = isNowLiked, totalLikes });
        }

        private async Task<JsonResult> ToggleFavoriteAsync(int userId, int? playId = null, int? courseId = null, int? masterId = null)
        {
            Favorite? existing = null;
            if (playId is int p)
                existing = await _context.Favorites.FirstOrDefaultAsync(f =>
                    f.UserId == userId && f.PlayId == p && f.CourseId == null && f.MasterId == null);
            else if (courseId is int c)
                existing = await _context.Favorites.FirstOrDefaultAsync(f =>
                    f.UserId == userId && f.CourseId == c && f.PlayId == null && f.MasterId == null);
            else if (masterId is int m)
                existing = await _context.Favorites.FirstOrDefaultAsync(f =>
                    f.UserId == userId && f.MasterId == m && f.PlayId == null && f.CourseId == null);
            else
                return Json(new { success = false, message = "参数无效" });

            bool isNowFavorited;
            if (existing != null)
            {
                _context.Favorites.Remove(existing);
                isNowFavorited = false;
            }
            else
            {
                _context.Favorites.Add(new Favorite { UserId = userId, PlayId = playId, CourseId = courseId, MasterId = masterId });
                isNowFavorited = true;
            }

            await _context.SaveChangesAsync();

            int totalFavorites;
            if (playId is int pCt)
                totalFavorites = await _context.Favorites.CountAsync(f =>
                    f.PlayId == pCt && f.CourseId == null && f.MasterId == null);
            else if (courseId is int cCt)
                totalFavorites = await _context.Favorites.CountAsync(f =>
                    f.CourseId == cCt && f.PlayId == null && f.MasterId == null);
            else if (masterId is int mCt)
                totalFavorites = await _context.Favorites.CountAsync(f =>
                    f.MasterId == mCt && f.PlayId == null && f.CourseId == null);
            else
                totalFavorites = 0;

            return Json(new { success = true, isFavorited = isNowFavorited, totalFavorites });
        }
        #endregion
    }
}