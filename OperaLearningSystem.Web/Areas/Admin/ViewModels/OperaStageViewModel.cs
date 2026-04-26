using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace OperaLearningSystem.Web.Areas.Admin.ViewModels;

public class OperaStageViewModel
{
    public int Id { get; set; }

    [Required]
    public int RegionId { get; set; }

    [Required(ErrorMessage = "戏台名称不能为空")]
    [Display(Name = "戏台名称")]
    public string Name { get; set; } = "";

    [Display(Name = "介绍")]
    public string? Introduction { get; set; }

    [Display(Name = "排序")]
    public int SortOrder { get; set; }

    [Display(Name = "照片")]
    public IFormFile? ImageFile { get; set; }

    public string? ExistingImageUrl { get; set; }

    public IEnumerable<SelectListItem> RegionOptions { get; set; } = new List<SelectListItem>();
}
