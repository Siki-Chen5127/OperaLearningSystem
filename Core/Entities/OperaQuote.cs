namespace OperaLearningSystem.Core.Entities
{
    public class OperaQuote
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        public int AuthorId { get; set; }
        public User Author { get; set; }
    }
}
