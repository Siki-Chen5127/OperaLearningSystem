using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.Areas.Admin.ViewModels
{
    public class CourseEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "课程名称不能为空")]
        [Display(Name = "课程名称")]
        public string Name { get; set; }

        [Display(Name = "课程描述")]
        public string Description { get; set; }

        [Display(Name = "视频链接")]
        [Url(ErrorMessage = "请输入有效的URL地址")]
        public string? VideoUrl { get; set; }

        [Display(Name = "是否为精选课程")]
        public bool IsFeatured { get; set; }

        [Required(ErrorMessage = "必须选择一个所属剧种")]
        [Display(Name = "所属剧种")]
        public int CategoryId { get; set; }

        [Display(Name = "当前封面")]
        public string? ExistingImageUrl { get; set; }

        [Display(Name = "更换封面")]
        public IFormFile? ImageFile { get; set; }     
        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
    }
}