namespace OperaLearningSystem.Core.Entities;

public class UserLearningProfile
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public double AbilityEstimate { get; set; } = 1.0;
    public int CorrectStreak { get; set; }
    public int WrongStreak { get; set; }
}
