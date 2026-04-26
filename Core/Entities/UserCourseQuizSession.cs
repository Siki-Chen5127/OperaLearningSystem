namespace OperaLearningSystem.Core.Entities;

/// <summary>
/// 学员在某课程下进行的一次固定题量考卷（进行中）。
/// </summary>
public class UserCourseQuizSession
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    /// <summary>本次考卷题目 Id 列表（JSON 数组，顺序固定）。</summary>
    public string QuestionIdsJson { get; set; } = "[]";
    /// <summary>当前待答题目在列表中的下标（0-based）。</summary>
    public int CurrentIndex { get; set; }
    public int CorrectCount { get; set; }
    public int WrongCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}
