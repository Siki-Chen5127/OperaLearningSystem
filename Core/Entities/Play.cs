namespace OperaLearningSystem.Core.Entities
{
    public class Play
    {
        public int Id { get; set; }
        public string Title { get; set; }       // 剧目名称
        public string Synopsis { get; set; }    // 剧情简介
        public string VideoUrl { get; set; }    // 演出视频链接
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public string ImageUrl { get; set; }
        public ICollection<Comment> Comments { get; set; }
        public List<Favorite> Favorites { get; set; } = new();
        public List<PlayMaster> PlayMasters { get; set; } = new();
        public List<Like> Likes { get; set; } = new();
        public int? SubmitterId { get; set; }
        public User? Submitter { get; set; }
        public int AuditStatus { get; set; } = 1;
    }
}
