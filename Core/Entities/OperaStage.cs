namespace OperaLearningSystem.Core.Entities;

public class OperaStage
{
    public int Id { get; set; }
    public int RegionId { get; set; }
    public OperaStageRegion Region { get; set; } = null!;
    public string Name { get; set; } = "";
    public string? Introduction { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public int? SubmitterId { get; set; }
    public User? Submitter { get; set; }
    public int AuditStatus { get; set; } = 1;
}
