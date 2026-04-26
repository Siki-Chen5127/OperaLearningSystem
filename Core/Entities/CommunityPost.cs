namespace OperaLearningSystem.Core.Entities
{
    public class CommunityPost
    {

        public int Id { get; set; }
        public string Title { get; set; } // 帖子标题
        public string Content { get; set; } // 帖子内容
        public int CategoryId { get; set; } // 关联分类
        public Category Category { get; set; } // 导航属性
        public DateTime CreatedTime { get; set; } = DateTime.Now; // 发布时间
        public int AuthorId { get; set; } // 外键
        public User Author { get; set; }
        public List<Comment> Comments { get; set; } = new();

        /// <summary>0 雅集 1 戏台打卡 2 百宝阁作品</summary>
        public int PostKind { get; set; }
        public string? TopicTags { get; set; }
        public string? MediaUrls { get; set; }
        public string? RegionLabel { get; set; }
        public List<Like> PostLikes { get; set; } = new();
    }
}