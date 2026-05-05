using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace OperaLearningSystem.Web.Controllers
{
    /// <summary>
    /// 社区页面控制器 (不处理任何表单提交数据)
    /// </summary>
    public class CommunityController : Controller
    {
        private readonly OperaDbContext _context;

        public CommunityController(OperaDbContext context)
        {
            _context = context;
        }

        // ================= 1. 页面路由 =================

        /// <summary>雅集 · 论坛主站</summary>
        public IActionResult Index()
        {
            // 1. 从数据库读取所有剧种分类 
            var categoryList = _context.Categories.ToList();

            ViewBag.Categories = new SelectList(categoryList, "Id", "Name");

            return View();
        }
        /// <summary>戏台打卡 · 行迹录</summary>
        [Authorize] // 强制登录才能看打卡页
        public IActionResult Checkin() => View();

        /// <summary>百宝阁 · 作品分享</summary>
        [Authorize]
        public IActionResult Works() => View();

        /// <summary>梨园心灯</summary>
        public IActionResult Lantern() => RedirectToAction("Index", "QuoteBoard");
        public IActionResult HeartLantern() => RedirectToAction("Index", "QuoteBoard");

        /// <summary>跳转到个人中心收藏夹</summary>
        [Authorize]
        public IActionResult Bookmarks() => LocalRedirect(Url.Action("UserCenter", "Account") + "#tab-postbm");

        // ================= 2. 详情页渲染 =================

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var post = await _context.CommunityPosts
                .Include(p => p.Author)
                .Include(p => p.Comments.OrderByDescending(c => c.CreatedAt))
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null) return NotFound();

            ViewBag.AllComments = post.Comments.ToList();
            return View(post);
        }
    }
}