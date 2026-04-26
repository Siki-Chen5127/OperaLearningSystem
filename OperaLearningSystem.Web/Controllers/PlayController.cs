using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OperaLearningSystem.Application.Services;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Web.Helpers;

namespace OperaLearningSystem.Web.Controllers
{
    public class PlayController : Controller
    {
        private readonly IPlayService _playService;
        private readonly UserManager<User> _userManager;
        private readonly OperaDbContext _db;

        public PlayController(IPlayService playService, UserManager<User> userManager, OperaDbContext db)
        {
            _playService = playService;
            _userManager = userManager;
            _db = db;
        }

        public async Task<IActionResult> Index(string searchString, int? categoryId, int pageNumber = 1)
        {
            int pageSize = 6;
            ViewData["CurrentFilter"] = searchString;
            ViewData["CategoryId"] = categoryId;

            var pagedResult = await _playService.GetPagedAsync(pageNumber, pageSize, searchString, categoryId, true);
            return View(pagedResult);
        }


        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var play = await _playService.GetPlayDetailsByIdAsync(id.Value);

            if (play == null) return NotFound();

            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(_userManager.GetUserId(User));
                ViewBag.IsLikedByCurrentUser = play.Likes.Any(l => l.UserId == userId);
                ViewBag.IsFavoritedByCurrentUser = play.Favorites.Any(f => f.UserId == userId);
            }
            else
            {
                ViewBag.IsLikedByCurrentUser = false;
                ViewBag.IsFavoritedByCurrentUser = false;
            }

            var allComments = play.Comments.ToList();
            ViewBag.AllComments = allComments;
            int? voteUserId = User.Identity.IsAuthenticated ? int.Parse(_userManager.GetUserId(User)) : null;
            ViewBag.CommentVoteStats = await CommentVoteStatsHelper.LoadAsync(_db, allComments.Select(c => c.Id), voteUserId);
            ViewBag.CommentCourseId = null;
            ViewBag.CommentPlayId = play.Id;
            ViewBag.CommentPostId = null;

            return View(play);
        }
    }
}