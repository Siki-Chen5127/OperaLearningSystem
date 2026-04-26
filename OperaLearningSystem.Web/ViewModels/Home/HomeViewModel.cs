using OperaLearningSystem.Core.Entities; // 引用命名空间

namespace OperaLearningSystem.Web.ViewModels.Home;

public class HomeViewModel
{
    public List<Play> RecommendedPlays { get; set; }
    public List<OperaLyric> Lyrics { get; set; }
}