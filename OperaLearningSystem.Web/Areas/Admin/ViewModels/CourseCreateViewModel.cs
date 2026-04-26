namespace OperaLearningSystem.Web.Areas.Admin.ViewModels
{
    using System.ComponentModel.DataAnnotations;

    public class CourseCreateViewModel
    {
        [Required(ErrorMessage = "课程名称不能为空")]
        [Display(Name = "课程名称")]
        public string Name { get; set; }

        [Required(ErrorMessage = "请选择所属剧种")]
        [Display(Name = "所属剧种")]
        public int CategoryId { get; set; }

        [Display(Name = "课程简介")]
        public string Description { get; set; }

        [Display(Name = "课程视频链接 (URL)")]
        [Url(ErrorMessage = "请输入有效的网址")]
        public string VideoUrl { get; set; }

        [Required(ErrorMessage = "请上传课程封面")]
        [Display(Name = "课程封面")]
        public IFormFile ImageFile { get; set; }

        [Display(Name = "设为精选课程")]
        public bool IsFeatured { get; set; }
    }
}