using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OperaLearningSystem.Core.Entities
{
    public class AdminApplication
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } // 申请理由，比如“我是资深票友，想帮忙录入数据”

        // 状态：0=待审核，1=已通过，2=已驳回
        public int Status { get; set; } = 0;

        public string? RejectReason { get; set; } // 驳回理由

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; } // 审批时间
    }
}