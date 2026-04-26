using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Hubs
{
    /// <summary>匿名可连接并接收广播；放飞心灯需登录（见 SendQuote）。</summary>
    [AllowAnonymous]
    public class QuoteHub : Hub
    {
        private readonly OperaDbContext _context;
        private readonly UserManager<User> _userManager;

        public QuoteHub(OperaDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public async Task SendQuote(string content)
        {
            // 验证内容
            if (string.IsNullOrWhiteSpace(content) || content.Length > 50)
                return;

            // 4. 获取当前登录的用户
            var authorId = int.Parse(_userManager.GetUserId(Context.User));
            var author = await _context.Users.FindAsync(authorId);

            if (author == null) return; // 安全检查

            // 5. 创建新的“戏词”实体
            var operaQuote = new OperaQuote
            {
                Content = content,
                AuthorId = authorId,
                CreatedTime = DateTime.UtcNow
            };

            // 6. 保存到数据库
            _context.OperaQuotes.Add(operaQuote);
            await _context.SaveChangesAsync();

            // 7. 准备要广播的数据
            var broadcastContent = operaQuote.Content;
            var broadcastAuthor = author.Nickname ?? author.UserName;
            var broadcastAvatar = author.AvatarUrl ?? "/images/default_avatar.png"; // 使用你已有的默认头像
            var broadcastTimestamp = operaQuote.CreatedTime;

            await Clients.All.SendAsync("ReceiveQuote",
                operaQuote.Id,
                broadcastContent,
                broadcastAuthor,
                broadcastAvatar,
                broadcastTimestamp);
        }
    }
}