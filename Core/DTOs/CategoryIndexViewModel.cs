using OperaLearningSystem.Core.Entities;

namespace OperaLearningSystem.Core.DTOs
{
    public class CategoryIndexViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public int PlayCount { get; set; }
        public int CourseCount { get; set; }
        public int MasterCount { get; set; }
    }
}