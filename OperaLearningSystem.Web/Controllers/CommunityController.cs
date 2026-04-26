using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Web.Helpers;
using OperaLearningSystem.Web.ViewModels.Community;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Controllers
{
    public class CommunityController : Controller
    {
        private readonly OperaDbContext _context;
        private readonly UserManager<User> _userManager;

        public CommunityController(OperaDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>雅集 · 论坛主站</summary>
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories.AsNoTracking().OrderBy(c => c.Id).ToListAsync(),
                "Id", "Name");
            return View();
        }

        /// <summary>戏台打卡 · 福台互通</summary>
        public IActionResult Checkin() => View();

        /// <summary>百宝阁 · 作品分享</summary>
        public IActionResult Works() => View();

        /// <summary>帖子收藏已并入个人中心；保留旧地址重定向。</summary>
        [Authorize]
        public IActionResult Bookmarks()
        {
            return LocalRedirect(Url.Action("UserCenter", "Account") + "#tab-postbm");
        }

        /// <summary>旧「天坛心灯」页已废；保留 URL 并跳转至梨园心灯（QuoteBoard）。</summary>
        public IActionResult Lantern() => RedirectToAction("Index", "QuoteBoard");

        // GET: 帖子详情
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.CommunityPosts
                .Include(p => p.Author)
                .Include(p => p.Comments.OrderByDescending(c => c.CreatedAt))
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            var allComments = post.Comments.ToList();
            ViewBag.AllComments = allComments;
            int? voteUserId = User.Identity.IsAuthenticated ? int.Parse(_userManager.GetUserId(User)) : null;
            ViewBag.CommentVoteStats = await CommentVoteStatsHelper.LoadAsync(_context, allComments.Select(c => c.Id), voteUserId);
            ViewBag.CommentCourseId = null;
            ViewBag.CommentPlayId = null;
            ViewBag.CommentPostId = post.Id;

            return View(post);
        }

        // POST: 发表评论
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "评论内容不能为空。";
                return RedirectToAction(nameof(Details), new { id = postId });
            }

            var post = await _context.CommunityPosts.FindAsync(postId);
            if (post == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            var comment = new Comment
            {
                PostId = postId,
                UserId = user.Id,
                Content = content,
                CreatedAt = DateTime.Now
            };

            _context.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "评论发表成功！";
            return RedirectToAction(nameof(Details), new { id = postId });
        }

        // GET: 发帖页面
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name");
            return View();
        }

        // POST: 发帖处理
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommunityPostCreateViewModel viewModel)
        {
            // 这里假设您有 CommunityPostCreateViewModel，如果没有，请将参数改为 CommunityPost 并相应调整代码
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                var post = new CommunityPost
                {
                    Title = viewModel.Title,
                    Content = viewModel.Content,
                    CategoryId = viewModel.CategoryId,
                    AuthorId = user.Id,
                    CreatedTime = DateTime.Now,
                    PostKind = 0
                };

                _context.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", viewModel.CategoryId);
            return View(viewModel);
        }

        // GET: 编辑页面 (修复：转为 ViewModel)
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.CommunityPosts.FindAsync(id);
            if (post == null) return NotFound();

            // 权限检查
            var user = await _userManager.GetUserAsync(User);
            if (post.AuthorId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // [修复点]：将 Entity 转换为 ViewModel
            var viewModel = new CommunityPostEditViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                CategoryId = post.CategoryId
            };

            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", post.CategoryId);
            return View(viewModel); // [修复点]：传给视图的是 ViewModel
        }

        // POST: 编辑处理 (修复：接收 ViewModel)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CommunityPostEditViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            var postToUpdate = await _context.CommunityPosts.FindAsync(id);
            if (postToUpdate == null) return NotFound();

            // 权限检查
            var user = await _userManager.GetUserAsync(User);
            if (postToUpdate.AuthorId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // [修复点]：将 ViewModel 的值更新回数据库实体
                    postToUpdate.Title = viewModel.Title;
                    postToUpdate.Content = viewModel.Content;
                    postToUpdate.CategoryId = viewModel.CategoryId;
                    // 注意：不更新 AuthorId 和 CreatedTime

                    _context.Update(postToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.CommunityPosts.Any(e => e.Id == viewModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Details), new { id = viewModel.Id });
            }
            ViewBag.Categories = new SelectList(await _context.Categories.ToListAsync(), "Id", "Name", viewModel.CategoryId);
            return View(viewModel);
        }

        // POST: 删除处理 (修复：级联删除评论)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.CommunityPosts.FindAsync(id);
            if (post == null) return NotFound();

            // 权限检查
            var user = await _userManager.GetUserAsync(User);
            if (post.AuthorId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // [修复点]：先查找并删除该帖子下的所有评论
            var comments = _context.Comments.Where(c => c.PostId == id);
            _context.Comments.RemoveRange(comments);

            // 然后再删除帖子
            _context.CommunityPosts.Remove(post);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EmbedStream()
        {
            var posts = await _context.CommunityPosts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Where(p => p.PostKind == 0)
                .OrderByDescending(p => p.CreatedTime)
                .Take(50)
                .ToListAsync();

            if (User.Identity?.IsAuthenticated == true)
            {
                var me = await _userManager.GetUserAsync(User);
                if (me != null && !string.IsNullOrWhiteSpace(me.Hobbies))
                {
                    var prefs = me.Hobbies.Split(new[] { '，', ',', '、', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    posts = posts.OrderByDescending(p => TagScore(p.TopicTags, prefs)).ThenByDescending(p => p.CreatedTime).ToList();
                }
            }

            return View(posts.Take(40).ToList());
        }

        private static int TagScore(string? tags, HashSet<string> prefs)
        {
            if (string.IsNullOrEmpty(tags) || prefs.Count == 0) return 0;
            return tags.Split(new[] { '，', ',', '、' }, StringSplitOptions.RemoveEmptyEntries)
                .Count(t => prefs.Contains(t.Trim()));
        }

        public IActionResult HeartLantern() => RedirectToAction("Index", "QuoteBoard");
    }
}