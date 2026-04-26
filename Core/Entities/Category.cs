namespace OperaLearningSystem.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int ParentId { get; set; }
    public string ImageUrl { get; set; }
    public string Description { get; set; }
    public string History { get; set; }

    // 导航属性：关联课程、社区帖子
    public ICollection<CommunityPost> Posts { get; set; }
    public List<Play> Plays { get; set; } = new();       // 一个剧种有多个剧目
    public List<Course> Courses { get; set; } = new();   // 一个剧种有多门课程
    public List<Master> Masters { get; set; } = new();   // 一个剧种有多个名家
    public int? SubmitterId { get; set; } // 提交人
    public User? Submitter { get; set; }
    public int AuditStatus { get; set; } = 1;

}

