namespace OperaLearningSystem.Core.Entities;
public class Like
{
    public int Id { get; set; }

    // 关联用户
    public int UserId { get; set; }
    public User User { get; set; }

    // 像 Favorite 一样，一个点赞可以属于多种类型的内容
    public int? PlayId { get; set; }   // 点赞剧目
    public Play Play { get; set; }

    public int? CourseId { get; set; }  // 点赞课程
    public Course Course { get; set; }

    public int? MasterId { get; set; } // 点赞名家
    public Master Master { get; set; }

    public int? CommentId { get; set; } // 甚至可以点赞评论
    public Comment Comment { get; set; }

    public int? CommunityPostId { get; set; }
    public CommunityPost? CommunityPost { get; set; }

    /// <summary>0 赞 1 鲜花 2 喝彩（社区帖）</summary>
    public byte ReactionKind { get; set; }
}
