namespace OperaLearningSystem.Core.Entities
{
    /// <summary>用户对留言的表态：1 赞、-1 踩；每用户每条留言最多一行。</summary>
    public class CommentVote
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public int CommentId { get; set; }
        public Comment Comment { get; set; } = null!;
        public short Value { get; set; }
    }
}
