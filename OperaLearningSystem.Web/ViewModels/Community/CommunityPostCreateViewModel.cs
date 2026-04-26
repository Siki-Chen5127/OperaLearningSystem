using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.ViewModels.Community
{
    public class CommunityPostCreateViewModel
    {
        [Required(ErrorMessage = "帖子标题不能为空。")]
        [StringLength(100, ErrorMessage = "标题长度不能超过100个字符。")]
        public string Title { get; set; }

        [Required(ErrorMessage = "帖子内容不能为空。")]
        public string Content { get; set; }

        [Required(ErrorMessage = "必须选择一个帖子分类。")]
        [Display(Name = "帖子分类")]
        public int CategoryId { get; set; }
    }
}