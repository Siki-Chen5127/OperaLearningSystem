using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.Areas.Admin.ViewModels
{
    public class CategoryEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "剧种名称不能为空")]
        [Display(Name = "剧种名称")]
        public string Name { get; set; }

        [Display(Name = "简介")]
        public string? Description { get; set; }
      
        [Display(Name = "历史源流")]
        public string? History { get; set; }
        [Display(Name = "当前图片")]
        public string? ExistingImageUrl { get; set; }

        [Display(Name = "上传新图片")]
        public IFormFile? ImageFile { get; set; }
    }
}