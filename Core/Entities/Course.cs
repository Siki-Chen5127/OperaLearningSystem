namespace OperaLearningSystem.Core.Entities
{
    public class Course
    {
        public int Id { get; set; }
        public string Name { get; set; } // 课程名称（如“京剧唱腔入门”）
        public string Description { get; set; } // 课程描述
        public string VideoUrl { get; set; } // 课程视频链接
        public string? BilibiliEmbedHtml { get; set; }
        public int CategoryId { get; set; } // 关联分类
        public bool IsFeatured { get; set; }
        public string ImageUrl { get; set; }
        public List<Comment> Comments { get; set; } = new();
        public List<Favorite> Favorites { get; set; } = new();
        public Category Category { get; set; } // 导航属性
        public List<Like> Likes { get; set; } = new();
        public List<QuizQuestion> QuizQuestions { get; set; } = new();
        public int? SubmitterId { get; set; }
        public User? Submitter { get; set; }
        public int AuditStatus { get; set; } = 1;


    }
}