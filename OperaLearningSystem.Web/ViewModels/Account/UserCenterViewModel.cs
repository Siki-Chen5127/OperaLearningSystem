using OperaLearningSystem.Core.Entities;
using System.Collections.Generic;

namespace OperaLearningSystem.Web.ViewModels.Account
{
    // 用于前端显示的统一卡片项
    public class UserContentItem
    {
        public int Id { get; set; }
        public string Title { get; set; }      // 统一显示名称 (Play.Title 或 Master.Name)
        public string ImageUrl { get; set; }   // 封面图
        public string ContentType { get; set; } // "Play", "Master", "Course"
        public string ControllerName { get; set; } // 用于生成跳转链接
        public string ActionName { get; set; }     // 通常是 "Details"
    }

    public class CourseQuizHistoryRow
    {
        public int CourseId { get; set; }
        public string CourseName { get; set; } = "";
        public int CorrectCount { get; set; }
        public int TotalCount { get; set; }
        public double AccuracyPercent { get; set; }
        public DateTime FinishedAt { get; set; }
    }

    /// <summary>雅集等社区帖子的「藏」书签（ReactionKind=3）。</summary>
    public class UserCommunityPostBookmarkItem
    {
        public int PostId { get; set; }
        public string Title { get; set; } = "";
        public string Excerpt { get; set; } = "";
        public string AuthorDisplay { get; set; } = "";
        public string? CategoryName { get; set; }
        public DateTime CreatedTime { get; set; }
    }

    public class UserCenterViewModel
    {
        public User User { get; set; }

        public List<UserContentItem> Favorites { get; set; } = new List<UserContentItem>();

        // 点赞记录
        public List<UserContentItem> Likes { get; set; } = new List<UserContentItem>();

        /// <summary>传习私塾课程考卷完成记录（最新在前）。</summary>
        public List<CourseQuizHistoryRow> CourseQuizHistory { get; set; } = new();

        public List<UserCommunityPostBookmarkItem> PostBookmarks { get; set; } = new();
    }
}