namespace OperaLearningSystem.Core.Entities
{
    public class Favorite
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int? PlayId { get; set; }   // 收藏剧目
        public int? CourseId { get; set; }  // 收藏课程
        public int? MasterId { get; set; } // 收藏名家
        public Master Master { get; set; }
        public Course Course { get; set; }
        public Play Play { get; set; }
    }
}
