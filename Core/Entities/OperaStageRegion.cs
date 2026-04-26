namespace OperaLearningSystem.Core.Entities;

public class OperaStageRegion
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public List<OperaStage> Stages { get; set; } = new();
}
