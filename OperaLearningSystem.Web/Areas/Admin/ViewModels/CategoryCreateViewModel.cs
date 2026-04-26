using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.Areas.Admin.ViewModels
{
    public class CategoryCreateViewModel
    {
        [Required(ErrorMessage = "剧种名称不能为空")]
        [Display(Name = "剧种名称")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "剧种简介不能为空")]
        [Display(Name = "简介")]
        public string Description { get; set; }

        [Required(ErrorMessage = "剧种历史不能为空")]
        [Display(Name = "历史")]
        public string History { get; set; }

        [Required(ErrorMessage = "请选择一张图片")]
        [Display(Name = "剧种图片")]
        public IFormFile ImageFile { get; set; }
    }
}