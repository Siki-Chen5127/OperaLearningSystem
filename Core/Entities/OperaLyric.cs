namespace OperaLearningSystem.Core.Entities
{
    public class OperaLyric
    {
        public int Id { get; set; }
        public string Content { get; set; }       // 戏词内容
        public string Interpretation { get; set; } // 唯美解读
        public int? PlayId { get; set; }          // 外键 (可为空，有些戏词可能通用)
        public Play Play { get; set; }            // 导航属性
        public string? SourceText { get; set; }
    }
}