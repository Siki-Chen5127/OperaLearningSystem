using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OperaLearningSystem.Core.Entities
{
    /// <summary>
    /// AI 角色表：存储不同数字人的prompt提示词
    /// </summary>
    public class AiCharacter
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty; // 角色名：如“虞姬”、“杜丽娘”、“小梨”

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty; // 一句话简介：如“楚霸王项羽之爱妾”

        [MaxLength(255)]
        public string AvatarUrl { get; set; } = string.Empty; // 头像路径

        [MaxLength(255)]
        public string BackgroundUrl { get; set; } = string.Empty; // 专属沉浸式背景图路径

        [Required]
        public string SystemPrompt { get; set; } = string.Empty; //  Prompt

        [MaxLength(500)]
        public string GreetingMessage { get; set; } = string.Empty; // 角色开场白（用户刚点进来的第一句话）

        public bool IsActive { get; set; } = true; // 是否启用该角色

        public int SortOrder { get; set; } = 0; // 排序（让谁排在前面）

        // 导航属性：一个角色可以有很多条对话记录
        public ICollection<AiChatMessage> ChatMessages { get; set; } = new List<AiChatMessage>();
    }
}