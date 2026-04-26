using System.Collections.Generic;
using OperaLearningSystem.Core.Entities;

namespace OperaLearningSystem.Web.ViewModels.Account
{
    public class AuditIndexViewModel
    {
        // 申请当管理员的
        public IEnumerable<AdminApplication> PendingApplications { get; set; }

        // Admin提交或修改的
        public IEnumerable<Play> PendingPlays { get; set; }
        public IEnumerable<Master> PendingMasters { get; set; }
        public IEnumerable<Category> PendingCategories { get; set; }
        public IEnumerable<Course> PendingCourses { get; set; }
    }
}