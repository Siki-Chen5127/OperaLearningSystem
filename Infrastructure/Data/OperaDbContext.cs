namespace OperaLearningSystem.Infrastructure.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;

public class OperaDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public OperaDbContext(DbContextOptions<OperaDbContext> options) : base(options) { }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Play> Plays { get; set; }
    public DbSet<Master> Masters { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CommunityPost> CommunityPosts { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<CommentVote> CommentVotes { get; set; }
    public DbSet<Favorite> Favorites { get; set; }
    public DbSet<Like> Likes { get; set; }
    public DbSet<PlayMaster> PlayMasters { get; set; }
    public DbSet<OperaQuote> OperaQuotes { get; set; }
    public DbSet<OperaLyric> OperaLyrics { get; set; }
    public DbSet<AiCharacter> AiCharacters { get; set; }
    public DbSet<AiChatMessage> AiChatMessages { get; set; }
    public DbSet<AdminApplication> AdminApplications { get; set; }
    public DbSet<QuizQuestion> QuizQuestions { get; set; }
    public DbSet<UserCourseQuizSession> UserCourseQuizSessions { get; set; }
    public DbSet<UserCourseQuizAttempt> UserCourseQuizAttempts { get; set; }
    public DbSet<UserLearningProfile> UserLearningProfiles { get; set; }
    public DbSet<UserBadge> UserBadges { get; set; }
    public DbSet<OperaStageRegion> OperaStageRegions { get; set; }
    public DbSet<OperaStage> OperaStages { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlayMaster>()
            .HasKey(pm => new { pm.PlayId, pm.MasterId });

        modelBuilder.Entity<PlayMaster>()
            .HasOne(pm => pm.Play)
            .WithMany(p => p.PlayMasters)
            .HasForeignKey(pm => pm.PlayId);

        modelBuilder.Entity<PlayMaster>()
            .HasOne(pm => pm.Master)
            .WithMany(m => m.PlayMasters)
            .HasForeignKey(pm => pm.MasterId);

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.Play)
            .WithMany(p => p.Favorites)
            .HasForeignKey(f => f.PlayId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.Course)
            .WithMany(c => c.Favorites)
            .HasForeignKey(f => f.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.Master)
            .WithMany(m => m.Favorites)
            .HasForeignKey(f => f.MasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Comment>()
            .Property(c => c.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CommentVote>()
            .HasOne(v => v.Comment)
            .WithMany(c => c.Votes)
            .HasForeignKey(v => v.CommentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CommentVote>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CommentVote>()
            .HasIndex(v => new { v.UserId, v.CommentId })
            .IsUnique();

        modelBuilder.Entity<CommunityPost>()
            .Property(p => p.CreatedTime)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
      
        modelBuilder.Entity<Play>()
            .HasOne(p => p.Category) 
            .WithMany(c => c.Plays)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Master>()
            .HasOne(m => m.Category) 
            .WithMany(c => c.Masters)
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Play>()
            .HasOne(p => p.Submitter)
            .WithMany(u => u.SubmittedPlays)
            .HasForeignKey(p => p.SubmitterId)
            .OnDelete(DeleteBehavior.SetNull); // 如果用户注销了，他提交的剧目依然保留，只是提交人变成null

        modelBuilder.Entity<Master>()
            .HasOne(m => m.Submitter)
            .WithMany(u => u.SubmittedMasters)
            .HasForeignKey(m => m.SubmitterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Course>()
            .HasOne(c => c.Submitter)
            .WithMany(u => u.SubmittedCourses)
            .HasForeignKey(c => c.SubmitterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.Submitter)
            .WithMany(u => u.SubmittedCategories)
            .HasForeignKey(c => c.SubmitterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AdminApplication>()
            .HasOne(a => a.User)
            .WithMany(u => u.AdminApplications)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserLearningProfile>()
            .HasKey(p => p.UserId);
        modelBuilder.Entity<UserLearningProfile>()
            .HasOne(p => p.User)
            .WithOne(u => u.LearningProfile)
            .HasForeignKey<UserLearningProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Like>()
            .HasOne(l => l.CommunityPost)
            .WithMany(p => p.PostLikes)
            .HasForeignKey(l => l.CommunityPostId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Like>()
            .HasIndex(l => new { l.UserId, l.CommunityPostId, l.ReactionKind })
            .IsUnique()
            .HasFilter("[CommunityPostId] IS NOT NULL");

        modelBuilder.Entity<OperaStageRegion>()
            .HasMany(r => r.Stages)
            .WithOne(s => s.Region)
            .HasForeignKey(s => s.RegionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OperaStage>()
            .HasOne(s => s.Submitter)
            .WithMany()
            .HasForeignKey(s => s.SubmitterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<QuizQuestion>()
            .HasOne(q => q.Course)
            .WithMany(c => c.QuizQuestions)
            .HasForeignKey(q => q.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserCourseQuizSession>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserCourseQuizSession>()
            .HasOne(s => s.Course)
            .WithMany()
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserCourseQuizAttempt>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserCourseQuizAttempt>()
            .HasOne(a => a.Course)
            .WithMany()
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserCourseQuizAttempt>()
            .HasIndex(a => new { a.UserId, a.FinishedAt });

        modelBuilder.Entity<UserCourseQuizSession>()
            .HasIndex(s => s.ExpiresAt);
    }
}