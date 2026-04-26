using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Web.ViewModels.AiRoleplay;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Controllers
{
    // 强制要求登录才能进行角色扮演（因为要存记忆）
    [Authorize]
    public class AiRoleplayController : Controller
    {
        private readonly OperaDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _config;

        public AiRoleplayController(OperaDbContext context, UserManager<User> userManager, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
        }

        // 1. 渲染页面与读取记忆
        public async Task<IActionResult> Index(int? characterId)
        {
            if (!characterId.HasValue) return RedirectToAction("Dresser");

            var characters = await _context.AiCharacters.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            if (!characters.Any()) return NotFound("暂无AI角色数据。");

            var activeChar = characterId.HasValue ? characters.FirstOrDefault(c => c.Id == characterId.Value) : characters.First();
            if (activeChar == null) activeChar = characters.First();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // 用户Session残留但数据库已重建，强制重新登录
                return RedirectToAction("Login", "Account", new { ReturnUrl = "/AiRoleplay" });
            }

            // 去数据库找这个用户和这个角色的所有历史记录
            var history = await _context.AiChatMessages
                .Where(m => m.UserId == currentUser.Id && m.CharacterId == activeChar.Id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            var viewModel = new AiRoleplayIndexViewModel
            {
                Characters = characters,
                ActiveCharacter = activeChar,
                ChatHistory = history // 把记忆传给前端
            };

            return View(viewModel);
        }

        // 接收前端发来的消息 DTO
        public class ChatRequestDto
        {
            public int CharacterId { get; set; }
            public string Message { get; set; }
        }

        // 2. 核心大模型对话接口
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto req)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var character = await _context.AiCharacters.FindAsync(req.CharacterId);
            if (currentUser == null || character == null || string.IsNullOrWhiteSpace(req.Message))
                return BadRequest("参数错误");

            // a. 将用户的消息存入数据库记忆
            var userMsg = new AiChatMessage { UserId = currentUser.Id, CharacterId = character.Id, Role = "user", Content = req.Message };
            _context.AiChatMessages.Add(userMsg);
            await _context.SaveChangesAsync();

            // b. 提取历史记忆喂给大模型（取最近10条，防止上下文过长）
            var history = await _context.AiChatMessages
                .Where(m => m.UserId == currentUser.Id && m.CharacterId == character.Id)
                .OrderByDescending(m => m.CreatedAt).Take(10).ToListAsync();
            history.Reverse(); // 倒序排回正常时间线

            // c. 组装 DeepSeek 的 Prompt
            var messagesArray = new System.Collections.Generic.List<object>
            {
                new { role = "system", content = character.SystemPrompt } // 注入灵魂
            };
            foreach (var msg in history)
            {
                messagesArray.Add(new { role = msg.Role, content = msg.Content });
            }

            // d. 呼叫 DeepSeek API
            string apiKey = _config["AiSettings:ApiKey"];
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestBody = new
            {
                model = "deepseek-chat", // DeepSeek 模型名
                messages = messagesArray,
                temperature = 0.7
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync("https://api.deepseek.com/chat/completions", content); // DeepSeek 官方地址

            if (!response.IsSuccessStatusCode) return StatusCode(500, "大模型 API 调用失败");

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var aiReply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            // e. 将 AI 的回复也存入数据库永久记忆
            var aiMsg = new AiChatMessage { UserId = currentUser.Id, CharacterId = character.Id, Role = "assistant", Content = aiReply };
            _context.AiChatMessages.Add(aiMsg);

            var trackedUser = await _context.Users.FindAsync(currentUser.Id);
            if (trackedUser != null)
                AppendDreamPersona(trackedUser, req.Message + " " + aiReply);

            await _context.SaveChangesAsync();

            return Json(new { reply = aiReply });
        }

        // 3. “时光倒流”接口：清空当前角色记忆
        [HttpPost]
        public async Task<IActionResult> ClearHistory([FromBody] int characterId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var history = await _context.AiChatMessages
                .Where(m => m.UserId == currentUser.Id && m.CharacterId == characterId).ToListAsync();

            _context.AiChatMessages.RemoveRange(history);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Dresser()
        {
            var characters = await _context.AiCharacters
                .Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync();
            return View(characters);
        }

        private static readonly string[] PersonaKeywords =
        {
            "昆曲", "京剧", "越剧", "黄梅戏", "豫剧", "梆子", "戏台", "机关", "升平署", "故宫",
            "牡丹亭", "霸王别姬", "长生殿", "西厢记", "脸谱", "水袖"
        };

        private static void AppendDreamPersona(User user, string blob)
        {
            if (string.IsNullOrEmpty(blob)) return;
            var hit = PersonaKeywords.Where(k => blob.Contains(k, StringComparison.Ordinal)).ToList();
            if (hit.Count == 0) return;
            var set = (user.DreamPersonaSummary ?? "")
                .Split('、', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.Ordinal);
            foreach (var h in hit) set.Add(h);
            user.DreamPersonaSummary = string.Join("、", set.Take(40));
        }
    }
}