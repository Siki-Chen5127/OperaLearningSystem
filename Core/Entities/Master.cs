namespace OperaLearningSystem.Core.Entities
{
    public class Master
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Introduction { get; set; } // 简介
        public string ImageUrl { get; set; }
        public int Rating { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public List<Favorite> Favorites { get; set; } = new();//一个名家可以被多个用户收藏
        public List<PlayMaster> PlayMasters { get; set; } = new();
        public List<Like> Likes { get; set; } = new();
        public int? SubmitterId { get; set; }
        public User? Submitter { get; set; }
        public int AuditStatus { get; set; } = 1;
    }
}