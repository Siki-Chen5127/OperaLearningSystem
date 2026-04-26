namespace OperaLearningSystem.Core.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; }     // 评论内容
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 关联用户
        public int UserId { get; set; }
        public User User { get; set; }
        public Play Play { get; set; }
        public int? PlayId { get; set; }       // 关联剧目
        public int? CourseId { get; set; }
        public Course Course { get; set; }     // 关联课程
        public int? PostId { get; set; }        // 关联社区帖子
        public CommunityPost Post { get; set; }

        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; } = new List<Comment>();
        public ICollection<CommentVote> Votes { get; set; } = new List<CommentVote>();
    }
}

