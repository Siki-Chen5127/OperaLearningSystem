using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.Areas.Admin.ViewModels
{
    public class MasterEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "名家姓名不能为空")]
        [Display(Name = "名家姓名")]
        public string Name { get; set; }

        [Display(Name = "简介")]
        public string Introduction { get; set; }

        [Display(Name = "推荐评级")]
        [Range(1, 5, ErrorMessage = "评级必须在1到5之间")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "必须选择一个所属剧种")]
        [Display(Name = "所属剧种")]
        public int CategoryId { get; set; }

        [Display(Name = "当前头像")]
        public string? ExistingImageUrl { get; set; }

        [Display(Name = "更换头像")]
        public IFormFile? ImageFile { get; set; }
        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
    }
}