using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.ViewModels.Community
{
    public class CommunityPostEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "请输入标题")]
        [StringLength(100, ErrorMessage = "标题不能超过100个字符")]
        [Display(Name = "帖子标题")]
        public string Title { get; set; }

        [Required(ErrorMessage = "请输入内容")]
        [Display(Name = "帖子内容")]
        public string Content { get; set; }

        [Display(Name = "所属分类")]
        public int CategoryId { get; set; }
    }
}