using System.Collections.Generic;
using OperaLearningSystem.Core.Entities; 

namespace OperaLearningSystem.Web.ViewModels.AiRoleplay
{
    public class AiRoleplayIndexViewModel
    {
        // 左侧栏：所有的 AI 角色列表
        public List<AiCharacter> Characters { get; set; } = new List<AiCharacter>();

        // 右侧聊天区：当前正在陪你聊天的那个角色
        public AiCharacter? ActiveCharacter { get; set; }
        // 存储当前角色与当前用户的过往聊天记忆
        public List<AiChatMessage> ChatHistory { get; set; } = new List<AiChatMessage>();
    }
}