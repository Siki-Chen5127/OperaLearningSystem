using OperaLearningSystem.Core.Entities;
using System.Collections.Generic;

namespace OperaLearningSystem.Web.Areas.Admin.ViewModels
{
    public class AdminHomeViewModel
    {
        // 顶部核心数据指标
        public int PlayCount { get; set; }
        public int UserCount { get; set; }
        public int AiChatCount { get; set; }
        public int CommunityPostCount { get; set; }
        public int PendingApplicationCount { get; set; }

        // 权限标识
        public bool IsSuperAdmin { get; set; }

        // 底部最新动态
        public IEnumerable<User> RecentUsers { get; set; }
        public IEnumerable<CommunityPost> RecentPosts { get; set; }

        // ECharts 数据源
        public string CategoryChartDataJson { get; set; }
        public string AiActivityDatesJson { get; set; }
        public string AiActivityCountsJson { get; set; }
        public string ModuleStatsJson { get; set; } // 雷达图数据
        public string UserInteractionJson { get; set; } // 互动环形图数据
    }
}