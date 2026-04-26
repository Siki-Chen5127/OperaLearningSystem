namespace OperaLearningSystem.Web.Areas.Admin.ViewModels
{
    using System.ComponentModel.DataAnnotations;

    public class MasterCreateViewModel
    {
        [Required(ErrorMessage = "名家姓名不能为空")]
        [Display(Name = "名家姓名")]
        public string Name { get; set; }

        [Required(ErrorMessage = "请选择所属剧种")]
        [Display(Name = "所属剧种")]
        public int CategoryId { get; set; }

        [Display(Name = "简介")]
        public string Introduction { get; set; }

        [Range(1, 5, ErrorMessage = "评级必须在1-5之间")]
        [Display(Name = "推荐评级")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "请上传名家照片")]
        [Display(Name = "名家照片")]
        public IFormFile ImageFile { get; set; }
    }
}