using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Web.Areas.Admin.ViewModels;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace OperaLearningSystem.Web.Areas.Admin.Controllers
{
    public class HomeController : BaseAdminController
    {
        private readonly OperaDbContext _context;

        public HomeController(OperaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. 判断当前登录用户是否为超级管理员
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            // 2. 初始化 ViewModel
            var viewModel = new AdminHomeViewModel
            {
                IsSuperAdmin = isSuperAdmin,
                PlayCount = await _context.Plays.CountAsync(),
                UserCount = await _context.Users.CountAsync(),
                AiChatCount = await _context.AiChatMessages.CountAsync(),
                CommunityPostCount = await _context.CommunityPosts.CountAsync()
            };

            // 3. 待审核统计（Status 为 int，假设 0 = Pending）
            if (isSuperAdmin)
            {
                viewModel.PendingApplicationCount = await _context.AdminApplications
                    .Where(a => a.Status == 0) // 0 表示待审核
                    .CountAsync();
            }

            // 4. 底部最新动态 (拉取最新5条用户和帖子)
            viewModel.RecentUsers = await _context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(5)
                .ToListAsync();

            viewModel.RecentPosts = await _context.CommunityPosts
                .Include(p => p.Author)
                .OrderByDescending(p => p.CreatedTime)
                .Take(5)
                .ToListAsync();

            // 5. 左侧图表：剧种剧目占比
            var categoryData = await _context.Plays
                .Where(p => p.Category != null)
                .GroupBy(p => p.Category.Name)
                .Select(g => new { name = g.Key, value = g.Count() })
                .ToListAsync();
            viewModel.CategoryChartDataJson = JsonSerializer.Serialize(categoryData);

            // 6. 右侧图表：近7天 AI 梦境沉浸频次
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

            // 7. 系统生态多维雷达图
            var courseCount = await _context.Courses.CountAsync();
            var masterCount = await _context.Masters.CountAsync();
            var moduleStats = new List<int>
            {
                viewModel.PlayCount,
                masterCount,
                courseCount,
                viewModel.UserCount,
                viewModel.CommunityPostCount
            };
            viewModel.ModuleStatsJson = JsonSerializer.Serialize(moduleStats);

            // 8. 用户互动环形图
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