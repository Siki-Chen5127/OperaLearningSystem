namespace OperaLearningSystem.Core.Entities;

public class QuizQuestion
{
    public int Id { get; set; }
    /// <summary>1 建筑 2 多媒体 3 综合</summary>
    public int QuestionType { get; set; }
    public double Difficulty { get; set; } = 1.0;
    public string Prompt { get; set; } = string.Empty;
    public string OptionsJson { get; set; } = "[]";
    public int CorrectIndex { get; set; }
    public string? ImageUrl { get; set; }
    public string? Tags { get; set; }
    /// <summary>所属课程专属题；null 表示全站台通用题库。</summary>
    public int? CourseId { get; set; }
    public Course? Course { get; set; }
    /// <summary>公布答案时的简短解析。</summary>
    public string? Explanation { get; set; }
}
