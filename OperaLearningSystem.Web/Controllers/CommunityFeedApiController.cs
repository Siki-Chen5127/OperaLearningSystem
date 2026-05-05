using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;
using System.Text.Json;

namespace OperaLearningSystem.Web.Controllers;

[Route("api/community-feed")]
[ApiController]
public class CommunityFeedApiController : ControllerBase
{
    private readonly OperaDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _env;
    private static string WordCloudCacheKey(int kind) => $"community_word_cloud_v5_kind_{kind}";

    public CommunityFeedApiController(
        OperaDbContext db,
        UserManager<User> userManager,
        IMemoryCache cache,
        IWebHostEnvironment env)
    {
        _db = db;
        _userManager = userManager;
        _cache = cache;
        _env = env;
    }
    //广场接口
    [HttpGet("recommended")]
    public async Task<IActionResult> Recommended(
        [FromQuery] int kind = 0,
        [FromQuery] int take = 20,
        [FromQuery] string sort = "smart",
        [FromQuery] string? region = null,
        [FromQuery] string? wc = null,
        CancellationToken ct = default)
    {
        User? user = null;
        if (User.Identity?.IsAuthenticated == true)
            user = await _userManager.GetUserAsync(User);

        kind = Math.Clamp(kind, 0, 2);
        take = Math.Clamp(take, 1, 50);

        var q = _db.CommunityPosts
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostLikes)
            .Include(p => p.Comments)
            .Where(p => p.PostKind == kind);

        if (!string.IsNullOrWhiteSpace(region))
            q = q.Where(p => p.RegionLabel != null && p.RegionLabel.Contains(region));

        if (!string.IsNullOrWhiteSpace(wc))
        {
            var needle = wc.Trim();
            if (needle.Length > 50)
                needle = needle[..50];

            var idsFromComments = await _db.Comments.AsNoTracking()
                .Where(c => c.PostId != null && c.Content != null && c.Content.Contains(needle))
                .Select(c => c.PostId!.Value)
                .Distinct()
                .ToListAsync(ct);

            var idsFromPosts = await q.Where(p =>
                    (p.Title != null && p.Title.Contains(needle)) ||
                    (p.Content != null && p.Content.Contains(needle)) ||
                    (p.TopicTags != null && p.TopicTags.Contains(needle)) ||
                    (p.RegionLabel != null && p.RegionLabel.Contains(needle)))
                .Select(p => p.Id)
                .ToListAsync(ct);

            var merged = new HashSet<int>(idsFromPosts);
            foreach (var id in idsFromComments)
                merged.Add(id);

            if (merged.Count == 0)
            {
                return Ok(Array.Empty<object>());
            }

            // 单次 Contains(可翻译为 SQL IN)，避免在 dotnet watch 下多条含闭包的 Where 触发 TypeLoadException
            var mergedList = merged.ToList();
            q = _db.CommunityPosts
                .AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.PostLikes)
                .Include(p => p.Comments)
                .Where(p => p.PostKind == kind && mergedList.Contains(p.Id));
            if (!string.IsNullOrWhiteSpace(region))
                q = q.Where(p => p.RegionLabel != null && p.RegionLabel.Contains(region));
        }

        var list = await q.Take(200).ToListAsync(ct);

        HashSet<string> prefs = new(StringComparer.OrdinalIgnoreCase);
        if (user != null && !string.IsNullOrWhiteSpace(user.Hobbies))
        {
            prefs = user.Hobbies.Split(new[] { '，', ',', '、', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        sort = (sort ?? "smart").Trim().ToLowerInvariant();
        list = sort switch
        {
            "time" => list.OrderByDescending(p => p.CreatedTime).Take(take).ToList(),
            "region" => list.OrderBy(p => p.RegionLabel ?? "zzzz").ThenByDescending(p => p.CreatedTime).Take(take).ToList(),
            "hot" => list.OrderByDescending(ScoreHot).ThenByDescending(p => p.CreatedTime).Take(take).ToList(),
            _ => list.OrderByDescending(p => ScorePost(p, prefs) * 10 + ScoreHot(p)).ThenByDescending(p => p.CreatedTime).Take(take).ToList()
        };

        var myPostReactionKinds = new HashSet<(int PostId, byte Kind)>();
        if (user != null)
        {
            var mine = await _db.Likes.AsNoTracking()
                .Where(l => l.UserId == user.Id && l.CommunityPostId != null)
                .Select(l => new { postId = l.CommunityPostId!.Value, l.ReactionKind })
                .ToListAsync(ct);
            myPostReactionKinds = mine.Select(x => (x.postId, x.ReactionKind)).ToHashSet();
        }

        var dto = list.Select(p =>
        {
            var statLike = p.PostLikes.Count(x => x.ReactionKind == 0);
            var statFlower = p.PostLikes.Count(x => x.ReactionKind == 1);
            var statCheer = p.PostLikes.Count(x => x.ReactionKind == 2);
            var statBookmark = p.PostLikes.Count(x => x.ReactionKind == 3);
            var statShare = p.PostLikes.Count(x => x.ReactionKind == 4);
            var statComments = p.Comments.Count;
            return new
            {
                p.Id,
                p.Title,
                p.Content,
                p.CreatedTime,
                p.PostKind,
                p.TopicTags,
                p.RegionLabel,
                mediaUrls = ParseMediaUrls(p.MediaUrls),
                author = p.Author?.Nickname ?? p.Author?.UserName,
                avatarUrl = string.IsNullOrWhiteSpace(p.Author?.AvatarUrl) ? "/images/default_avatar.png" : p.Author!.AvatarUrl,
                category = p.Category?.Name,
                stats = new
                {
                    like = statLike,
                    flower = statFlower,
                    cheer = statCheer,
                    bookmark = statBookmark,
                    share = statShare,
                    comments = statComments
                },
                mine = new
                {
                    like = myPostReactionKinds.Contains((p.Id, 0)),
                    flower = myPostReactionKinds.Contains((p.Id, 1)),
                    cheer = myPostReactionKinds.Contains((p.Id, 2)),
                    bookmark = myPostReactionKinds.Contains((p.Id, 3)),
                    share = myPostReactionKinds.Contains((p.Id, 4))
                }
            };
        });
        return Ok(dto);
    }

    private async Task<int> ResolveCategoryId(CancellationToken ct)
    {
        var id = await _db.Categories.OrderBy(c => c.Id).Select(c => c.Id).FirstOrDefaultAsync(ct);
        return id == 0 ? 1 : id;
    }

    private static int ScorePost(CommunityPost p, HashSet<string> prefs)
    {
        if (prefs.Count == 0 || string.IsNullOrEmpty(p.TopicTags)) return 0;
        var tags = p.TopicTags.Split(new[] { '，', ',', '、' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim());
        return tags.Count(t => prefs.Contains(t));
    }

    private static int ScoreHot(CommunityPost p)
    {
        var likes = p.PostLikes.Count;
        var comments = p.Comments.Count;
        return likes + comments * 2;
    }

    private static List<string> ParseMediaUrls(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        raw = raw.Trim();
        if (raw.StartsWith("["))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(raw);
                return parsed?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
            }
            catch
            {
            }
        }

        return raw.Split(new[] { '\n', '\r', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().ToList();
    }


    /// <summary>
    /// 获取用户专属记录（打卡/百宝阁）
    /// URL: GET /api/community-feed/mine?kind=1
    /// </summary>

    /// <summary>
    /// 删除自己的卷宗
    /// URL: DELETE /api/community-feed/{id}
    /// </summary>

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMyPost(int id, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var post = await _db.CommunityPosts.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (post == null) return NotFound("该行迹已不存在");

        // 越权校验：防止黑客调用接口删别人的帖子
        if (post.AuthorId != user.Id && !User.IsInRole("Admin"))
            return Forbid("无权焚毁他人的行迹");

        // 级联删除相关的评论 (如果你配置了外键级联删除，这一步可省略，但手动删更稳妥)
        var comments = _db.Comments.Where(c => c.PostId == id);
        _db.Comments.RemoveRange(comments);

        // 删除相关的点赞/收藏
        var likes = _db.Likes.Where(l => l.CommunityPostId == id);
        _db.Likes.RemoveRange(likes);

        // 焚毁主帖
        _db.CommunityPosts.Remove(post);
        await _db.SaveChangesAsync(ct);

        return Ok(new { success = true });
    }
    [Authorize]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMyPosts([FromQuery] int kind = 1, CancellationToken ct = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var list = await _db.CommunityPosts.AsNoTracking()
            .Where(p => p.PostKind == kind && p.AuthorId == user.Id)
            .OrderByDescending(p => p.CreatedTime)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Content,
                p.CreatedTime,
                p.PostKind,
                p.TopicTags,
                p.RegionLabel,
                mediaUrls = ParseMediaUrls(p.MediaUrls),
                author = user.Nickname ?? user.UserName,
                avatarUrl = string.IsNullOrWhiteSpace(user.AvatarUrl) ? "/images/default_avatar.png" : user.AvatarUrl
            })
            .ToListAsync(ct);

        return Ok(list);
    }

    /// <summary>雅集发帖（PostKind 0）</summary>
    [Authorize]
    [HttpPost("lyrics-post")]
    public async Task<IActionResult> CreateLyricsPost([FromBody] LyricsPostDto dto, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("标题与正文不能为空");

        var post = new CommunityPost
        {
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            CategoryId = dto.CategoryId > 0 ? dto.CategoryId : await ResolveCategoryId(ct),
            AuthorId = user.Id,
            CreatedTime = DateTime.Now,
            PostKind = 0
        };
        _db.CommunityPosts.Add(post);
        await _db.SaveChangesAsync(ct);
        return Ok(new { id = post.Id });
    }

    [Authorize]
    [HttpPost("checkin")]
    public async Task<IActionResult> CreateCheckIn([FromBody] CheckInDto dto, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("标题与正文不能为空");

        var post = new CommunityPost
        {
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            CategoryId = dto.CategoryId > 0 ? dto.CategoryId : await ResolveCategoryId(ct),
            AuthorId = user.Id,
            CreatedTime = DateTime.Now,
            PostKind = 1,
            TopicTags = dto.TopicTags,
            RegionLabel = dto.RegionLabel,
            MediaUrls = dto.MediaUrlsJson
        };
        _db.CommunityPosts.Add(post);
        await _db.SaveChangesAsync(ct);
        return Ok(new { id = post.Id });
    }

    [Authorize]
    [HttpPost("work")]
    public async Task<IActionResult> CreateWork([FromBody] WorkDto dto, CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("标题与正文不能为空");

        var post = new CommunityPost
        {
            Title = dto.Title.Trim(),
            Content = dto.Content.Trim(),
            CategoryId = dto.CategoryId > 0 ? dto.CategoryId : await ResolveCategoryId(ct),
            AuthorId = user.Id,
            CreatedTime = DateTime.Now,
            PostKind = 2,
            TopicTags = dto.TopicTags,
            MediaUrls = dto.MediaUrlsJson
        };
        _db.CommunityPosts.Add(post);
        await _db.SaveChangesAsync(ct);
        return Ok(new { id = post.Id });
    }


    [Authorize]
    [HttpPost("react/{postId:int}")]
    public async Task<IActionResult> React(int postId, [FromQuery] byte kind = 1, CancellationToken ct = default)
    {
        if (kind > 4) return BadRequest();
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var like = await _db.Likes.FirstOrDefaultAsync(l =>
            l.UserId == user.Id && l.CommunityPostId == postId && l.ReactionKind == kind, ct);
        if (like != null)
        {
            _db.Likes.Remove(like);
            await _db.SaveChangesAsync(ct);
            return Ok(new { added = false });
        }

        _db.Likes.Add(new Like
        {
            UserId = user.Id,
            CommunityPostId = postId,
            ReactionKind = kind
        });
        await _db.SaveChangesAsync(ct);
        return Ok(new { added = true });
    }

    [Authorize]
    [RequestSizeLimit(30_000_000)]
    [HttpPost("upload-media")]
    public async Task<IActionResult> UploadMedia([FromForm] IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length <= 0) return BadRequest("文件为空");
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm", ".mov" };
        if (!allowed.Contains(ext)) return BadRequest("不支持的文件格式");

        var folder = Path.Combine(_env.WebRootPath, "uploads", "community", DateTime.Now.ToString("yyyyMM"));
        Directory.CreateDirectory(folder);
        var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
        var savePath = Path.Combine(folder, fileName);
        await using (var fs = new FileStream(savePath, FileMode.Create))
            await file.CopyToAsync(fs, ct);

        var url = $"/uploads/community/{DateTime.Now:yyyyMM}/{fileName}";
        return Ok(new { url });
    }

    [HttpGet("comments/{postId:int}")]
    public async Task<IActionResult> Comments(int postId, [FromQuery] int take = 30, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        var comments = await _db.Comments.AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.PostId == postId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(take)
            .Select(c => new
            {
                c.Id,
                c.Content,
                c.CreatedAt,
                author = c.User.Nickname ?? c.User.UserName
            })
            .ToListAsync(ct);
        return Ok(comments);
    }

    [Authorize]
    [HttpPost("comments/{postId:int}")]
    public async Task<IActionResult> CreateComment(int postId, [FromBody] CommentDto dto, CancellationToken ct = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Content)) return BadRequest("评论不能为空");
        var exists = await _db.CommunityPosts.AnyAsync(p => p.Id == postId, ct);
        if (!exists) return NotFound();

        var comment = new Comment
        {
            UserId = user.Id,
            PostId = postId,
            Content = dto.Content.Trim(),
            CreatedAt = DateTime.Now
        };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync(ct);
        return Ok(new { id = comment.Id });
    }

    [HttpGet("word-cloud")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> WordCloud([FromQuery] int kind = 0, CancellationToken ct = default)
    {
        Response.Headers.CacheControl = "no-store";

        kind = Math.Clamp(kind, 0, 2);
        var cacheKey = WordCloudCacheKey(kind);
        if (_cache.TryGetValue(cacheKey, out List<WordCloudEntry>? cached) && cached is { Count: > 0 })
            return Ok(cached);

        // 帖子 + 该 kind 下联帖评论（词云点击筛选用 recommended?wc=，会匹配正文或评论）
        var commentTexts = await (from c in _db.Comments.AsNoTracking()
                                 join p in _db.CommunityPosts.AsNoTracking() on c.PostId equals p.Id
                                 where p.PostKind == kind
                                 orderby c.CreatedAt descending
                                 select c.Content)
            .Take(800)
            .ToListAsync(ct);

        var posts = await _db.CommunityPosts.AsNoTracking()
            .Where(p => p.PostKind == kind)
            .OrderByDescending(p => p.CreatedTime)
            .Take(250)
            .Select(p => new { p.Title, p.Content, p.TopicTags, p.RegionLabel })
            .ToListAsync(ct);

        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        void AddFromText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            text = text.Trim();
            if (text.Length > 800)
                text = text[..800];
            foreach (var w in TokenizeRough(text))
            {
                if (w.Length < 2) continue;
                freq[w] = freq.TryGetValue(w, out var n) ? n + 1 : 1;
            }
        }

        foreach (var t in commentTexts)
            AddFromText(t);

        foreach (var p in posts)
        {
            AddFromText(p.Title);
            AddFromText(StripHtmlLoose(p.Content));
            AddFromText(p.TopicTags);
            AddFromText(p.RegionLabel);
        }

        var top = freq.OrderByDescending(kv => kv.Value).Take(60)
            .Select(kv => new WordCloudEntry(kv.Key, kv.Value)).ToList();

        if (top.Count > 0)
            _cache.Set(cacheKey, top, TimeSpan.FromMinutes(45));
        return Ok(top);
    }

    private record WordCloudEntry(string text, int value);

    private static string StripHtmlLoose(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        return Regex.Replace(html, "<.*?>", " ", RegexOptions.Singleline | RegexOptions.IgnoreCase).Trim();
    }

    private static IEnumerable<string> TokenizeRough(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) yield break;
        foreach (Match m in Regex.Matches(s, @"[\u4e00-\u9fa5]{2,4}|[a-zA-Z]{2,}|\d{2,}"))
            yield return m.Value;
    }

    public class LyricsPostDto
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public int CategoryId { get; set; }
    }

    public class CheckInDto
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public int CategoryId { get; set; }
        public string? TopicTags { get; set; }
        public string? RegionLabel { get; set; }
        public string? MediaUrlsJson { get; set; }
    }

    public class WorkDto
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public int CategoryId { get; set; }
        public string? TopicTags { get; set; }
        public string? MediaUrlsJson { get; set; }
    }

    public class CommentDto
    {
        public string Content { get; set; } = "";
    }
}
