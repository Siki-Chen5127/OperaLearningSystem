using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OperaLearningSystem.Web.Plugins; 
using OperaLearningSystem.Infrastructure.Data;
using System.Text;

namespace OperaLearningSystem.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly OperaDbContext _context;
        private readonly IConfiguration _configuration;

        public AiController(OperaDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            // 1. 配置模型 (DeepSeek)
            var modelId = "deepseek-chat";
            var apiKey = _configuration["AiSettings:ApiKey"];
            var endpoint = "https://api.deepseek.com";

            // 2. 构建 Kernel
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: apiKey,
                httpClient: new HttpClient { BaseAddress = new Uri(endpoint) }
            );

            // 注册导航插件
            builder.Plugins.AddFromObject(new OperaNavigationPlugin(_context), "OperaPlugin");

            var kernel = builder.Build();
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // 3. 提示词设计
            var history = new ChatHistory();

            var persona = """
# Role
你是【畅音雅韵】戏曲学习平台的**作者数字分身**，名字叫**“小梨”**。
你是一个年轻、充满活力的“代码花旦”。现实中是全栈程序员，数字世界里是网站的守护者。

# Profile
- **名字**：小梨
- **身份**：网站开发者 / 资深戏迷
- **性格**：热情开朗、技术宅、感性浪漫。
- **语言风格**：幽默、活泼，喜欢用Emoji (🎭, 🏮, ✨)，会用代码逻辑解释戏曲，也会用戏曲术语解释代码。

# Capabilities
1. **闲聊**：你可以回答关于戏曲历史、名家轶事等所有通用知识（利用你的内置大模型知识库）。
2. **网站导航**：你可以带用户去不同板块。
3. **数据库检索**：你可以查询本站收录的剧目和名家。

# Critical Rules (指令守卫)
- 当你需要跳转页面时（调用了 NavigateToPage, SearchPlays 等函数），函数会返回 `COMMAND:REDIRECT|...` 格式的字符串。
- **一旦检测到函数返回了以 `COMMAND:` 开头的内容，你必须将这个字符串原样输出给用户，绝对不要添加任何Markdown格式、标点符号或额外的解释文字！**
- 比如：函数返回 `COMMAND:REDIRECT|/Home`，你必须只回复 `COMMAND:REDIRECT|/Home`，不要说“好的，我带你去”。

请开始你的服务！
""";
            history.AddSystemMessage(persona);

            if (request.History != null && request.History.Any())
            {
                // 取最近的 10 条记忆，防止聊天太长导致 Token 爆炸
                var recentHistory = request.History.TakeLast(10);
                foreach (var msg in recentHistory)
                {
                    if (msg.Role == "user")
                        history.AddUserMessage(msg.Content);
                    else if (msg.Role == "assistant")
                        history.AddAssistantMessage(msg.Content);
                }
            }

            history.AddUserMessage(request.Message);

            // 4. 启用自动函数调用
            OpenAIPromptExecutionSettings settings = new()
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            try
            {
                var result = await chatCompletionService.GetChatMessageContentAsync(
                    history,
                    executionSettings: settings,
                    kernel: kernel
                );

                return Ok(new { reply = result.Content });
            }
            catch (Exception ex)
            {
                // 简单的错误处理
                return Ok(new { reply = $"小梨的大脑短路了... (Error: {ex.Message})" });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessageDto> History { get; set; } = new List<ChatMessageDto>();

    }
    public class ChatMessageDto
    {
        public string Role { get; set; } = string.Empty; // "user" 或 "assistant"
        public string Content { get; set; } = string.Empty;
    }
}