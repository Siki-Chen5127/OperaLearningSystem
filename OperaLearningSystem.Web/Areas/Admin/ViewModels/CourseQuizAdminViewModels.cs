using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.Areas.Admin.ViewModels;

public class CourseQuizQuestionsPageViewModel
{
    public int CourseId { get; set; }
    public string CourseName { get; set; } = "";
    public List<CourseQuizQuestionRow> Questions { get; set; } = new();
}

public class CourseQuizQuestionRow
{
    public int Id { get; set; }
    public string PromptPreview { get; set; } = "";
    public int CorrectIndex { get; set; }
    public string? Tags { get; set; }
}

public class CourseQuizQuestionEditViewModel
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public string CourseName { get; set; } = "";

    [Required(ErrorMessage = "请输入题干")]
    [Display(Name = "题干")]
    public string Prompt { get; set; } = "";

    [Display(Name = "选项甲")]
    [Required(ErrorMessage = "请填写四个选项")]
    public string Option0 { get; set; } = "";

    [Display(Name = "选项乙")]
    [Required(ErrorMessage = "请填写选项乙")]
    public string Option1 { get; set; } = "";

    [Display(Name = "选项丙")]
    [Required(ErrorMessage = "请填写选项丙")]
    public string Option2 { get; set; } = "";

    [Display(Name = "选项丁")]
    [Required(ErrorMessage = "请填写选项丁")]
    public string Option3 { get; set; } = "";

    [Range(0, 3, ErrorMessage = "正确项须为 0～3，对应甲～丁")]
    [Display(Name = "正确选项（0=甲，1=乙，2=丙，3=丁）")]
    public int CorrectIndex { get; set; }

    [Display(Name = "答案解析")]
    public string? Explanation { get; set; }

    [Display(Name = "题型代码")]
    public int QuestionType { get; set; } = 4;

    [Display(Name = "难度")]
    public double Difficulty { get; set; } = 1.0;
}
