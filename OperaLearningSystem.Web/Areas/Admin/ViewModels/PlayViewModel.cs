using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.Areas.Admin.ViewModels
{
    public class PlayViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "剧目名称不能为空")]
        [Display(Name = "剧目名称")]
        public string Title { get; set; }

        [Display(Name = "剧情简介")]
        public string Synopsis { get; set; }

        [Display(Name = "视频链接")]
        [Url(ErrorMessage = "请输入有效的URL地址")]
        public string VideoUrl { get; set; }

        [Required(ErrorMessage = "必须选择一个所属剧种")]
        [Display(Name = "所属剧种")]
        public int CategoryId { get; set; }

        [Display(Name = "关联名家")]
        public List<int>? SelectedMasterIds { get; set; } = new List<int>();

        [Display(Name = "剧目封面图")]
        [DataType(DataType.Upload)]
        public IFormFile? ImageFile { get; set; }


        public string ExistingImageUrl { get; set; }

        public IEnumerable<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> MasterOptions { get; set; } = new List<SelectListItem>();
    }
}
