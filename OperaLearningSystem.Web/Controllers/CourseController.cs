using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.DTOs;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Web.Helpers;

namespace OperaLearningSystem.Web.Controllers
{
    public class CourseController : Controller
    {
        private readonly ICourseService _courseService;
        private readonly UserManager<User> _userManager;
        private readonly OperaDbContext _db;


        public CourseController(ICourseService courseService, UserManager<User> userManager, OperaDbContext db)
        {
            _courseService = courseService;
            _userManager = userManager;
            _db = db;
        }
        public async Task<IActionResult> Index(string searchString, int? categoryId, int pageNumber = 1)
        {
            searchString = string.IsNullOrWhiteSpace(searchString) ? null : searchString.Trim();
            ViewData["CurrentFilter"] = searchString;
            ViewData["CategoryId"] = categoryId;

            const int pageSize = 12;

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                ViewData["CourseBrowseMode"] = "search";
                var pagedByCategory = await _courseService.GetPagedAsync(pageNumber, pageSize, searchString, categoryId, true);
                return View(pagedByCategory);
            }

            if (searchString is null)
            {
                var spotlight = await _courseService.GetRandomSpotlightCoursesAsync(6);
                var spotlightResult = new PagedResult<Course>
                {
                    Items = spotlight,
                    TotalItems = spotlight.Count,
                    PageNumber = 1,
                    PageSize = spotlight.Count > 0 ? spotlight.Count : 1
                };
                ViewData["CourseBrowseMode"] = "spotlight";
                return View(spotlightResult);
            }

            ViewData["CourseBrowseMode"] = "search";
            var pagedResult = await _courseService.GetPagedAsync(pageNumber, pageSize, searchString, null, true);
            return View(pagedResult);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _courseService.GetCourseDetailsByIdAsync(id.Value);

            if (course == null) return NotFound();

            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(_userManager.GetUserId(User));
                ViewBag.IsLikedByCurrentUser = course.Likes.Any(l => l.UserId == userId);
                ViewBag.IsFavoritedByCurrentUser = course.Favorites.Any(f => f.UserId == userId);
            }
            else
            {
                ViewBag.IsLikedByCurrentUser = false;
                ViewBag.IsFavoritedByCurrentUser = false;
            }

            var allComments = course.Comments.ToList();
            ViewBag.AllComments = allComments;
            int? voteUserId = User.Identity.IsAuthenticated ? int.Parse(_userManager.GetUserId(User)) : null;
            ViewBag.CommentVoteStats = await CommentVoteStatsHelper.LoadAsync(_db, allComments.Select(c => c.Id), voteUserId);
            ViewBag.CommentCourseId = course.Id;
            ViewBag.CommentPlayId = null;
            ViewBag.CommentPostId = null;

            return View(course);
        }

        [Authorize]
        public async Task<IActionResult> Study(int? id)
        {
            if (id == null) return NotFound();
            var course = await _courseService.GetCourseDetailsByIdAsync(id.Value);
            if (course == null) return NotFound();
            return View(course);
        }

        [HttpGet]
        public async Task<IActionResult> DetailsJson(int id)
        {
            var c = await _courseService.GetCourseDetailsByIdAsync(id);
            if (c == null) return NotFound();
            return Json(new
            {
                c.Id, c.Name, c.Description,
                category = c.Category?.Name ?? "",
                commentCount = c.Comments?.Count ?? 0,
                studyUrl = Url.Action("Study", "Course", new { id = c.Id }),
                detailsUrl = Url.Action("Details", "Course", new { id = c.Id })
            });
        }
    }
}