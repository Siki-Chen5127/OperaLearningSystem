using System.Text.Json;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using OperaLearningSystem.Core.Entities;

namespace OperaLearningSystem.Web.Services;

/// <summary>
/// 根据课程名称与简介调用大模型生成单选题，写入 <see cref="QuizQuestion"/>（不落库，由调用方保存）。
/// </summary>
public class CourseQuizAiService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CourseQuizAiService> _logger;

    public CourseQuizAiService(IConfiguration configuration, ILogger<CourseQuizAiService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<QuizQuestion>> GenerateForCourseAsync(
        int courseId,
        string courseName,
        string description,
        int questionCount,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(description)) description = courseName;
        questionCount = Math.Clamp(questionCount, 3, 8);

        var apiKey = _configuration["AiSettings:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("未配置 AiSettings:ApiKey，无法调用大模型。");

        var modelId = _configuration["AiSettings:ChatModel"] ?? "deepseek-chat";
        var endpoint = _configuration["AiSettings:ChatEndpoint"] ?? "https://api.deepseek.com";

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey,
            httpClient: new HttpClient { BaseAddress = new Uri(endpoint) });

        var kernel = builder.Build();
        var chat = kernel.GetRequiredService<IChatCompletionService>();

        var system = """
你是中国传统戏曲学习平台「畅音雅韵」的命题官，负责根据课程内容命制测验单选题。

# 输出纪律
- 只输出一个 JSON 数组，不要 Markdown 代码围栏，不要前言或后记。
- JSON 须可被 C# System.Text.Json 直接反序列化。
""";

        var user = $@"# 任务
根据下列课程信息，命制 {questionCount} 道单选题，用于检验学员是否掌握本课要点。

# 课程名称
{courseName}

# 课程简介 / 视频内容简介（命题唯一依据）
{description.Trim()}

# 命题要求
1. 每道题必须能从上述简介中合理推断或明确涉及，禁止泛泛常识题与课程无关。
2. 每题恰好 4 个选项，文字简洁；有且仅有 1 个正确。
3. 题干独立完整，不要写「根据上文」「本课提到」等模糊指代。
4. 覆盖不同知识点，避免重复问法。
5. correctIndex 为 0-3，对应 options 数组下标。
6. 只输出 JSON 数组本体，共 {questionCount} 个对象，不要 Markdown 围栏。

# JSON 格式示例（字段名与结构必须一致）
[
  {{
    ""prompt"": ""题干"",
    ""options"": [""选项A"",""选项B"",""选项C"",""选项D""],
    ""correctIndex"": 0,
    ""explanation"": ""不超过80字的解析，说明为何正确选项成立""
  }}
]
";

        var history = new ChatHistory();
        history.AddSystemMessage(system);
        history.AddUserMessage(user);

        var content = await chat.GetChatMessageContentAsync(history, cancellationToken: ct);
        var raw = content.Content?.Trim() ?? "";

        var json = ExtractJsonArray(raw);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        List<AiQuizItem>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<AiQuizItem>>(json, opts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI 考卷 JSON 解析失败: {Snippet}", json.Length > 400 ? json[..400] : json);
            throw new InvalidOperationException("大模型返回的内容无法解析为题目 JSON，请稍后再试或精简课程简介。");
        }

        if (items == null || items.Count == 0)
            throw new InvalidOperationException("大模型未返回有效题目。");

        var result = new List<QuizQuestion>();
        foreach (var item in items.Take(questionCount))
        {
            if (string.IsNullOrWhiteSpace(item.Prompt) || item.Options is not { Count: >= 4 })
                continue;
            var opts4 = item.Options.Take(4).Select(o => o?.Trim() ?? "").ToArray();
            var ci = Math.Clamp(item.CorrectIndex, 0, 3);
            result.Add(new QuizQuestion
            {
                QuestionType = 4,
                Difficulty = 1.0,
                CourseId = courseId,
                Prompt = item.Prompt.Trim(),
                OptionsJson = JsonSerializer.Serialize(opts4),
                CorrectIndex = ci,
                Explanation = string.IsNullOrWhiteSpace(item.Explanation) ? null : item.Explanation.Trim(),
                Tags = "ai_generated,course"
            });
        }

        if (result.Count < 3)
            throw new InvalidOperationException("生成的有效题目不足 3 道，请补充更具体的课程简介后重试。");

        return result;
    }

    private static string ExtractJsonArray(string raw)
    {
        var t = raw.Trim();
        if (t.StartsWith("```", StringComparison.Ordinal))
        {
            var lines = t.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (lines.Length > 2)
                t = string.Join('\n', lines.Skip(1).TakeWhile(l => !l.StartsWith("```", StringComparison.Ordinal)));
        }
        var start = t.IndexOf('[');
        var end = t.LastIndexOf(']');
        if (start >= 0 && end > start)
            return t[start..(end + 1)];
        return t;
    }

    private sealed class AiQuizItem
    {
        public string Prompt { get; set; } = "";
        public List<string>? Options { get; set; }
        public int CorrectIndex { get; set; }
        public string? Explanation { get; set; }
    }
}
