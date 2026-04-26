namespace OperaLearningSystem.Web.Areas.Admin.ViewModels
{
    public class PlayIndexViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string CategoryName { get; set; }
        public List<string> MasterNames { get; set; } = new List<string>();
    }
}