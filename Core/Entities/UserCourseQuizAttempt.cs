namespace OperaLearningSystem.Core.Entities;

/// <summary>
/// 学员完成某课程一次考卷后的记录（用于个人中心学习进度）。
/// </summary>
public class UserCourseQuizAttempt
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int CorrectCount { get; set; }
    public int TotalCount { get; set; }
    public DateTime FinishedAt { get; set; } = DateTime.UtcNow;
}
