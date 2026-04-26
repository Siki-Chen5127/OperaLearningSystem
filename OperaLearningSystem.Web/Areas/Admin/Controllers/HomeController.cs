using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Web.Areas.Admin.ViewModels;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Areas.Admin.Controllers
{
    public class HomeController : BaseAdminController
    {
        private readonly OperaDbContext _context;

        // 注入数据库上下文
        public HomeController(OperaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. 判断当前登录用户是否为超级管理员
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            // 2. 基础统计数据
            var viewModel = new AdminHomeViewModel
            {
                IsSuperAdmin = isSuperAdmin,
                PlayCount = await _context.Plays.CountAsync(),
                UserCount = await _context.Users.CountAsync(),
                AiChatCount = await _context.AiChatMessages.CountAsync(),
                CommunityPostCount = await _context.CommunityPosts.CountAsync()
            };

            // 如果是超级管理员，额外查询待审核的申请数
            if (isSuperAdmin)
            {
                viewModel.PendingApplicationCount = await _context.AdminApplications.CountAsync();
            }

            // 3. 底部列表数据 (拉取最新5条)
            viewModel.RecentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt) 
                .Take(5)
                .ToListAsync();

            viewModel.RecentPosts = await _context.CommunityPosts
                .Include(p => p.Author) // 联表拉取发帖人信息
                .OrderByDescending(p => p.CreatedTime)
                .Take(5)
                .ToListAsync();

            // 4. 左侧图表数据：剧种剧目占比 (南丁格尔玫瑰图)
            var categoryData = await _context.Plays
                .Where(p => p.Category != null)
                .GroupBy(p => p.Category.Name)
                .Select(g => new { name = g.Key, value = g.Count() })
                .ToListAsync();
            viewModel.CategoryChartDataJson = JsonSerializer.Serialize(categoryData);

            // 5. 右侧图表数据：近7天 AI 梦境沉浸频次走势
            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-i)).Reverse().ToList();
            var aiData = await _context.AiChatMessages
                .Where(m => m.CreatedAt >= DateTime.Today.AddDays(-7))
                .GroupBy(m => m.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var aiCounts = last7Days.Select(date => aiData.FirstOrDefault(d => d.Date == date)?.Count ?? 0).ToList();
            var aiDates = last7Days.Select(d => d.ToString("MM-dd")).ToList();

            viewModel.AiActivityDatesJson = JsonSerializer.Serialize(aiDates);
            viewModel.AiActivityCountsJson = JsonSerializer.Serialize(aiCounts);

            //6. 系统生态多维雷达图 (统计核心模块的总量)
            var courseCount = await _context.Courses.CountAsync();
            var masterCount = await _context.Masters.CountAsync();
            var moduleStats = new List<int> {
                viewModel.PlayCount,
                masterCount,
                courseCount,
                viewModel.UserCount,
                viewModel.CommunityPostCount
            };
            viewModel.ModuleStatsJson = JsonSerializer.Serialize(moduleStats);

            // 7. 用户互动行为环形图 (统计发帖、评论、收藏、点赞)
            var commentCount = await _context.Comments.CountAsync();
            var favoriteCount = await _context.Favorites.CountAsync();
            var likeCount = await _context.Likes.CountAsync();

            var interactionData = new List<object>
            {
                new { name = "雅集发帖", value = viewModel.CommunityPostCount },
                new { name = "戏迷评论", value = commentCount },
                new { name = "收藏剧目", value = favoriteCount },
                new { name = "点赞支持", value = likeCount }
            };
            viewModel.UserInteractionJson = JsonSerializer.Serialize(interactionData);

            return View(viewModel);
        }
    }
}