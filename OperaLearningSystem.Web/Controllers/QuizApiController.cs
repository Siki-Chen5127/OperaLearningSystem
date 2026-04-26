using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;

namespace OperaLearningSystem.Web.Controllers;

[Authorize]
[Route("api/quiz")]
[ApiController]
public class QuizApiController : ControllerBase
{
    private const int DefaultExamQuestionCount = 5;
    private readonly OperaDbContext _db;
    private readonly UserManager<User> _userManager;

    public QuizApiController(OperaDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>全站通用练习题（旧接口，传习页已改用课程卷）。</summary>
    [HttpGet("next")]
    public async Task<IActionResult> Next(CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var profile = await GetOrCreateProfileAsync(user.Id, ct);

        var targetDiff = Math.Clamp(profile.AbilityEstimate, 0.4, 2.5);
        var pool = await _db.QuizQuestions.AsNoTracking()
            .Where(q => q.CourseId == null)
            .ToListAsync(ct);
        var q = pool.OrderBy(x => Math.Abs(x.Difficulty - targetDiff)).ThenBy(_ => Guid.NewGuid()).FirstOrDefault();

        if (q == null) return Ok(new { ok = false, message = "题库尚未配置" });

        return Ok(BuildQuestionPayload(q));
    }

    public class AnswerDto
    {
        public int QuestionId { get; set; }
        public int SelectedIndex { get; set; }
    }

    [HttpPost("answer")]
    public async Task<IActionResult> Answer([FromBody] AnswerDto dto, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var q = await _db.QuizQuestions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.QuestionId, ct);
        if (q == null) return NotFound();

        var profile = await GetOrCreateProfileAsync(user.Id, ct);

        var correct = dto.SelectedIndex == q.CorrectIndex;
        if (correct)
        {
            profile.CorrectStreak++;
            profile.WrongStreak = 0;
            profile.AbilityEstimate = Math.Min(2.5, profile.AbilityEstimate + 0.12);
        }
        else
        {
            profile.WrongStreak++;
            profile.CorrectStreak = 0;
            profile.AbilityEstimate = Math.Max(0.4, profile.AbilityEstimate - 0.18);
        }

        await _db.SaveChangesAsync(ct);

        var badgeEarned = false;
        if (correct && profile.CorrectStreak >= 5)
        {
            const string code = "scholar_streak_5";
            if (!await _db.UserBadges.AnyAsync(b => b.UserId == user.Id && b.BadgeCode == code, ct))
            {
                _db.UserBadges.Add(new UserBadge { UserId = user.Id, BadgeCode = code });
                await _db.SaveChangesAsync(ct);
                badgeEarned = true;
            }
        }

        var expl = correct
            ? (string.IsNullOrWhiteSpace(q.Explanation) ? "善哉，已记入修习册。" : q.Explanation)
            : (string.IsNullOrWhiteSpace(q.Explanation)
                ? $"正解为选项 {((char)('A' + q.CorrectIndex))}。"
                : $"正解为选项 {((char)('A' + q.CorrectIndex))}。{q.Explanation}");

        return Ok(new { correct, explanation = expl, profile.AbilityEstimate, badgeEarned });
    }

    public class CourseStartDto
    {
        public int CourseId { get; set; }
    }

    /// <summary>开始本课程一次固定题量考卷；题目仅来自 CourseId 匹配的题库。</summary>
    [HttpPost("course/start")]
    public async Task<IActionResult> CourseStart([FromBody] CourseStartDto dto, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == dto.CourseId, ct);
        if (course == null) return NotFound(new { ok = false, message = "课程不存在" });

        var pool = await _db.QuizQuestions.AsNoTracking()
            .Where(q => q.CourseId == dto.CourseId)
            .ToListAsync(ct);
        if (pool.Count == 0)
            return Ok(new
            {
                ok = false,
                message = "本课程尚未配置专属题库。请管理员在后台填写课程简介后使用「AI 生成考卷」。"
            });

        var take = Math.Min(DefaultExamQuestionCount, pool.Count);
        var rnd = new Random();
        var picked = pool.OrderBy(_ => rnd.Next()).Take(take).Select(q => q.Id).ToList();

        await _db.UserCourseQuizSessions.Where(s => s.UserId == user.Id && s.CourseId == dto.CourseId)
            .ExecuteDeleteAsync(ct);

        var session = new UserCourseQuizSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CourseId = dto.CourseId,
            QuestionIdsJson = JsonSerializer.Serialize(picked),
            CurrentIndex = 0,
            CorrectCount = 0,
            WrongCount = 0,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };
        _db.UserCourseQuizSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        var first = picked[0];
        var q = pool.First(x => x.Id == first);
        var payload = BuildQuestionPayload(q);
        return Ok(new
        {
            ok = true,
            sessionId = session.Id,
            total = take,
            current = 1,
            question = payload
        });
    }

    public class CourseAnswerDto
    {
        public Guid SessionId { get; set; }
        public int QuestionId { get; set; }
        public int SelectedIndex { get; set; }
    }

    [HttpPost("course/answer")]
    public async Task<IActionResult> CourseAnswer([FromBody] CourseAnswerDto dto, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var session = await _db.UserCourseQuizSessions
            .FirstOrDefaultAsync(s => s.Id == dto.SessionId && s.UserId == user.Id, ct);
        if (session == null) return NotFound(new { ok = false, message = "会话已失效或未找到。" });
        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _db.UserCourseQuizSessions.Remove(session);
            await _db.SaveChangesAsync(ct);
            return BadRequest(new { ok = false, message = "考卷已超时，请重新开始。" });
        }

        List<int> ids;
        try { ids = JsonSerializer.Deserialize<List<int>>(session.QuestionIdsJson) ?? new List<int>(); }
        catch { return BadRequest(new { ok = false, message = "会话数据损坏。" }); }

        if (ids.Count == 0 || session.CurrentIndex >= ids.Count)
            return BadRequest(new { ok = false, message = "本卷已答完，请关闭后查看个人中心修习记录。" });

        if (ids[session.CurrentIndex] != dto.QuestionId)
            return BadRequest(new { ok = false, message = "题目顺序不匹配，请刷新页面重新开始。" });

        var q = await _db.QuizQuestions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dto.QuestionId, ct);
        if (q == null) return NotFound();

        var correct = dto.SelectedIndex == q.CorrectIndex;
        if (correct) session.CorrectCount++;
        else session.WrongCount++;

        var profile = await GetOrCreateProfileAsync(user.Id, ct);
        if (correct)
        {
            profile.CorrectStreak++;
            profile.WrongStreak = 0;
            profile.AbilityEstimate = Math.Min(2.5, profile.AbilityEstimate + 0.08);
        }
        else
        {
            profile.WrongStreak++;
            profile.CorrectStreak = 0;
            profile.AbilityEstimate = Math.Max(0.4, profile.AbilityEstimate - 0.1);
        }

        var explanation = correct
            ? (string.IsNullOrWhiteSpace(q.Explanation) ? "答得好，已记入修习册。" : q.Explanation)
            : (string.IsNullOrWhiteSpace(q.Explanation)
                ? $"正解为「{OptionLetter(q.CorrectIndex)}」。"
                : $"正解为「{OptionLetter(q.CorrectIndex)}」。{q.Explanation}");

        session.CurrentIndex++;

        if (session.CurrentIndex >= ids.Count)
        {
            var attempt = new UserCourseQuizAttempt
            {
                UserId = user.Id,
                CourseId = session.CourseId,
                CorrectCount = session.CorrectCount,
                TotalCount = ids.Count,
                FinishedAt = DateTime.UtcNow
            };
            _db.UserCourseQuizAttempts.Add(attempt);

            var courseName = await _db.Courses.AsNoTracking()
                .Where(c => c.Id == session.CourseId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(ct) ?? "";

            var acc = Math.Round(100.0 * session.CorrectCount / ids.Count, 1);
            var badgeEarned = false;
            if (session.CorrectCount == ids.Count)
            {
                const string code = "传习全对";
                if (!await _db.UserBadges.AnyAsync(b => b.UserId == user.Id && b.BadgeCode == code, ct))
                {
                    _db.UserBadges.Add(new UserBadge { UserId = user.Id, BadgeCode = code });
                    badgeEarned = true;
                }
            }

            _db.UserCourseQuizSessions.Remove(session);
            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                correct,
                explanation,
                sessionComplete = true,
                badgeEarned,
                summary = new
                {
                    courseName,
                    correctCount = session.CorrectCount,
                    totalCount = ids.Count,
                    accuracyPercent = acc,
                    certificate = $"于 {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC 完成「{courseName}」传习考卷，正确 {session.CorrectCount}/{ids.Count}（{acc}%）。"
                }
            });
        }

        var nextId = ids[session.CurrentIndex];
        var nextQ = await _db.QuizQuestions.AsNoTracking().FirstAsync(x => x.Id == nextId, ct);
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            correct,
            explanation,
            sessionComplete = false,
            progress = new { current = session.CurrentIndex + 1, total = ids.Count },
            nextQuestion = BuildQuestionPayload(nextQ)
        });
    }

    private async Task<UserLearningProfile> GetOrCreateProfileAsync(int userId, CancellationToken ct)
    {
        var profile = await _db.UserLearningProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile == null)
        {
            profile = new UserLearningProfile { UserId = userId, AbilityEstimate = 1.0 };
            _db.UserLearningProfiles.Add(profile);
            await _db.SaveChangesAsync(ct);
        }
        return profile;
    }

    private static string OptionLetter(int idx) => ((char)('A' + Math.Clamp(idx, 0, 25))).ToString();

    private static object BuildQuestionPayload(QuizQuestion q)
    {
        string[] options;
        try { options = JsonSerializer.Deserialize<string[]>(q.OptionsJson) ?? Array.Empty<string>(); }
        catch { options = Array.Empty<string>(); }

        return new
        {
            ok = true,
            id = q.Id,
            type = q.QuestionType,
            prompt = q.Prompt,
            options,
            imageUrl = q.ImageUrl,
            difficulty = q.Difficulty
        };
    }
}
