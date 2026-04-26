using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.Areas.Admin.ViewModels
{
    public class AiCharacterFormViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "请填写角色名")]
        [MaxLength(50)]
        [Display(Name = "角色名")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        [Display(Name = "简介")]
        public string Description { get; set; } = string.Empty;

        [MaxLength(255)]
        [Display(Name = "头像/剧照路径")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "上传剧照")]
        public IFormFile? AvatarFile { get; set; }

        [MaxLength(255)]
        [Display(Name = "沉浸背景图路径")]
        public string? BackgroundUrl { get; set; }

        [Display(Name = "上传背景图")]
        public IFormFile? BackgroundFile { get; set; }

        [Required(ErrorMessage = "请填写系统提示词")]
        [Display(Name = "系统提示词（人设）")]
        public string SystemPrompt { get; set; } = string.Empty;

        [MaxLength(500)]
        [Display(Name = "开场白")]
        public string? GreetingMessage { get; set; }

        [Display(Name = "启用")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "排序")]
        public int SortOrder { get; set; }

        public string? ExistingAvatarDisplay { get; set; }
        public string? ExistingBackgroundDisplay { get; set; }
    }
}
