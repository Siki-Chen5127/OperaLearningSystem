using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Core.Entities
{
    public class User : IdentityUser<int> // 核心：继承 IdentityUser<int>
    {
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<CommunityPost> CommunityPosts { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
        public List<Favorite> Favorites { get; set; } = new();
        public List<Like> Likes { get; set; } = new();
        public List<AdminApplication> AdminApplications { get; set; } = new();
        public List<Play> SubmittedPlays { get; set; } = new();
        public List<Master> SubmittedMasters { get; set; } = new();
        public List<Course> SubmittedCourses { get; set; } = new();
        public List<Category> SubmittedCategories { get; set; } = new();

        [StringLength(50)]
        public string? Nickname { get; set; } // 昵称
        public DateTime? BirthDate { get; set; } // 出生年月
        [StringLength(10)]
        public string? Gender { get; set; } // 性别
        [StringLength(50)]
        public string? Province { get; set; } // 籍贯 (省份)
        [StringLength(300)]
        public string? Bio { get; set; } // 个人简介
        [StringLength(200)]
        public string? Hobbies { get; set; }

        [StringLength(2000)]
        public string? DreamPersonaSummary { get; set; }

        public UserLearningProfile? LearningProfile { get; set; }
    }

}