namespace OperaLearningSystem.Core.Entities
{
    public class PlayMaster
    {
        public Master Master { get; set; }
        public Play Play { get; set; }
        public int MasterId { get; set; }
        public int PlayId { get; set; }

    }
}
