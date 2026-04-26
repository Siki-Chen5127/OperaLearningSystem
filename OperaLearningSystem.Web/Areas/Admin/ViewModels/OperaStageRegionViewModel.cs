using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Web.Areas.Admin.ViewModels;

public class OperaStageRegionViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "区域名称不能为空")]
    [Display(Name = "区域名称（地图分区）")]
    public string Name { get; set; } = "";

    [Display(Name = "排序")]
    public int SortOrder { get; set; }
}
