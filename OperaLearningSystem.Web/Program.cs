using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Application.Services;
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Web.Filters;
using OperaLearningSystem.Web.Hubs;
using OperaLearningSystem.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 服务注册区域---

#region 1. 注册数据库上下文服务
builder.Services.AddDbContext<OperaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("OperaLearningSystem.Infrastructure")));
// 戏台知识：工厂注册，避免热重载/程序集加载顺序导致接口未解析
builder.Services.AddScoped<IOperaStageService>(sp =>
    new OperaStageService(sp.GetRequiredService<OperaDbContext>()));
#endregion

#region 2. 注册完整的 ASP.NET Core Identity 服务
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
    .AddEntityFrameworkStores<OperaDbContext>()
    .AddDefaultTokenProviders();
#endregion

#region 3. 注册 AutoMapper 服务
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
#endregion

#region 4. 注册所有的自定义业务服务
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ICommunityPostService, CommunityPostService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IMasterService, MasterService>();
builder.Services.AddScoped<IPlayService, PlayService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSignalR();
builder.Services.AddHostedService<LyricWordCloudRefreshService>();
builder.Services.AddScoped<CourseQuizAiService>();
#endregion

#region 5. 注册MVC与Razor Pages框架服务
var mvcBuilder = builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<ExecutionTimeLogFilter>();
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
#endregion


// 中间件配置区域
var app = builder.Build();

// 初始化数据库和种子数据 ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<OperaDbContext>();

        await context.Database.EnsureCreatedAsync();
        await CourseQuizSchemaPatcher.EnsureAsync(context);

        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

        // --- 1. 确保 SuperAdmin和 Admin角色都存在---
        string[] roleNames = { "SuperAdmin", "Admin" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }
        }

        // --- 2.设定全站唯一的超级管理员账号 ---
        var superAdminEmail = "admin@liyuan.com";
        var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);

        if (superAdminUser == null)
        {
            superAdminUser = new User
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                Nickname = "\u68a8\u56ed\u638c\u95e8" 
            };
            await userManager.CreateAsync(superAdminUser, "123456");
        }

        // --- 3. 给最高权限 ---
        if (!await userManager.IsInRoleAsync(superAdminUser, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
        }

        // 后台 [Authorize(Roles = "Admin")] 需要 Admin；与 SuperAdmin 一并赋予，避免只挂超管角色进不了后台
        if (!await userManager.IsInRoleAsync(superAdminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(superAdminUser, "Admin");
        }

        
        // --- 5. Quiz questions (增量) ---
        if (!context.QuizQuestions.Any())
        {
            var qs = new[]
            {
                Q(1, 1.0, "\u6545\u5bab\u7545\u97f3\u9601\u5bff\u53f0\u5927\u620f\u697c\u4e3b\u4f53\u53ef\u8fbe\u51e0\u5c42\u76f8\u901a\u4ee5\u6f14\u51fa\u795e\u4ed9\u9898\u6750\uff1f", new[]{"\u4e00\u5c42","\u4e8c\u5c42","\u4e09\u5c42","\u56db\u5c42"}, 2, "\u53e4\u5efa,\u620f\u53f0"),
                Q(1, 1.4, "\u5fb7\u548c\u56ed\u5927\u620f\u697c\u4f4d\u4e8e\u54ea\u5ea7\u7687\u5bb6\u56ed\u6797\uff1f", new[]{"\u9890\u548c\u56ed","\u627f\u5fb7\u907f\u6691\u5c71\u5e84","\u5706\u660e\u56ed","\u5929\u575b"}, 0, "\u53e4\u5efa"),
                Q(2, 0.9, "\u620f\u66f2\u884c\u5f53\u201c\u82b1\u8138\u201d\u591a\u5bf9\u5e94\u54ea\u4e00\u7c7b\u89d2\u8272\uff1f", new[]{"\u751f","\u65e6","\u51c0","\u4e11"}, 2, "\u620f\u66f2\u5e38\u8bc6"),
                Q(3, 1.8, "\u4e09\u5c42\u620f\u53f0\u4e2d\u5e38\u8bbe\u6c34\u4e95\u3001\u673a\u5173\uff0c\u4e3b\u8981\u7528\u4e8e\u8868\u73b0\u4f55\u79cd\u821e\u53f0\u6548\u679c\uff1f", new[]{"\u6b66\u6253\u5bf9\u653b","\u6c34\u6cd5\u4e0e\u5347\u5929\u5165\u5730","\u4e50\u961f\u4f34\u594f","\u89c2\u4f17\u4e92\u52a8"}, 1, "\u8de8\u754c"),
                Q(1, 0.8, "\u6606\u66f2\u300a\u7261\u4e39\u4ead\u300b\u5c5e\u4e8e\u54ea\u4e2a\u5267\u79cd\uff1f", new[]{"\u4eac\u5267","\u6606\u66f2","\u7ca4\u5267","\u8d8a\u5267"}, 1, "\u620f\u66f2\u5e38\u8bc6"),
                Q(1, 1.2, "\u7545\u97f3\u9601\u4e09\u5c42\u4e2d\u6700\u5927\u7684\u620f\u53f0\u53eb\u4ec0\u4e48\uff1f", new[]{"\u5bff\u53f0","\u7984\u53f0","\u798f\u53f0","\u666f\u53f0"}, 0, "\u53e4\u5efa,\u620f\u53f0"),
                Q(1, 1.5, "\u7545\u97f3\u9601\u620f\u53f0\u5730\u4e95\u7684\u4e3b\u8981\u529f\u80fd\u662f\u4ec0\u4e48\uff1f", new[]{"\u6392\u6c34","\u5347\u964d\u6f14\u5458","\u5b58\u653e\u9053\u5177","\u89c2\u4f17\u5165\u573a"}, 1, "\u53e4\u5efa,\u673a\u5173"),
                Q(2, 1.0, "\u56db\u5927\u540d\u65e6\u4e2d\u4ee5\u6885\u6d3e\u95fb\u540d\u7684\u662f\u54ea\u4f4d\uff1f", new[]{"\u6885\u5170\u82b3","\u7a0b\u781a\u79cb","\u8340\u6167\u751f","\u5c1a\u5c0f\u4e91"}, 0, "\u620f\u66f2\u5e38\u8bc6,\u540d\u5bb6"),
                Q(2, 1.6, "\u6e05\u4ee3\u5347\u5e73\u7f72\u7684\u4e3b\u8981\u804c\u80fd\u662f\u4ec0\u4e48\uff1f", new[]{"\u7ba1\u7406\u5bab\u5ef7\u6f14\u620f\u4e8b\u52a1","\u7ba1\u7406\u5fa1\u81b3\u623f","\u7ba1\u7406\u5bab\u5ef7\u536b\u961f","\u7ba1\u7406\u7687\u5bb6\u56ed\u6797"}, 0, "\u5386\u53f2,\u5bab\u5ef7"),
                Q(3, 2.0, "\u4e2d\u56fd\u4f20\u7edf\u53e4\u620f\u53f0\u4e3b\u4f53\u7ed3\u6784\u901a\u5e38\u91c7\u7528\u4ec0\u4e48\u5efa\u9020\u65b9\u5f0f\uff1f", new[]{"\u6728\u7ed3\u6784\u69bc\u536f","\u94a2\u7b4b\u6df7\u51dd\u571f","\u7816\u77f3\u62f1\u5238","\u7af9\u7f16\u6ce5\u5899"}, 0, "\u8de8\u754c,\u5efa\u7b51"),
                Q(2, 1.3, "\u4eac\u5267\u8138\u8c31\u4e2d\u7ea2\u8272\u901a\u5e38\u4ee3\u8868\u4ec0\u4e48\u6027\u683c\u7279\u5f81\uff1f", new[]{"\u5fe0\u4e49\u52c7\u6b66","\u5978\u8be8\u591a\u7591","\u521a\u76f4\u66b4\u70c8","\u5e74\u8001\u7a33\u91cd"}, 0, "\u620f\u66f2\u5e38\u8bc6,\u8138\u8c31"),
                Q(3, 2.2, "\u627f\u5fb7\u907f\u6691\u5c71\u5e84\u6e05\u97f3\u9601\u620f\u53f0\u4e0e\u7545\u97f3\u9601\u76f8\u6bd4\u72ec\u7279\u5904\u5728\u4e8e\uff1f", new[]{"\u7ed3\u5408\u5c71\u6c34\u56ed\u6797","\u91c7\u7528\u94c1\u8d28\u7ed3\u6784","\u8bbe\u6709\u65cb\u8f6c\u821e\u53f0","\u53ef\u5bb9\u7eb3\u4e07\u4eba"}, 0, "\u8de8\u754c,\u53e4\u5efa"),
            };
            context.QuizQuestions.AddRange(qs);
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

static QuizQuestion Q(int type, double diff, string prompt, string[] opts, int correct, string tags)
{
    return new QuizQuestion
    {
        QuestionType = type, Difficulty = diff, Prompt = prompt,
        OptionsJson = System.Text.Json.JsonSerializer.Serialize(opts),
        CorrectIndex = correct, Tags = tags
    };
}

// 配置HTTP请求处理管道
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 在 UseStaticFiles 之前直接提供 .gltf：默认 StaticFileMiddleware 对无 MIME 映射的扩展名会跳过文件（表现为 404）。
// 仅靠 FileExtensionContentTypeProvider 时，若 watch 未完整重启进程，仍可能读到旧行为；此处保证总能从 WebRoot 读出文件。
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    var webRoot = app.Environment.WebRootPath;
    if (string.IsNullOrEmpty(webRoot)
        || path == null
        || !path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase)
        || !HttpMethods.IsGet(context.Request.Method))
    {
        await next();
        return;
    }

    var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length == 0)
    {
        await next();
        return;
    }

    var parts = new string[segments.Length + 1];
    parts[0] = webRoot;
    Array.Copy(segments, 0, parts, 1, segments.Length);
    var combined = Path.GetFullPath(Path.Combine(parts));
    var root = Path.GetFullPath(webRoot);
    if (combined.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(combined))
    {
        context.Response.ContentType = "model/gltf+json";
        await context.Response.SendFileAsync(combined);
        return;
    }

    await next();
});

var staticFileProvider = new FileExtensionContentTypeProvider();
staticFileProvider.Mappings[".gltf"] = "model/gltf+json";
staticFileProvider.Mappings[".glb"] = "model/gltf-binary";
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = staticFileProvider,
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 配置路由规则
app.MapControllerRoute(
    name: "Admin",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<QuoteHub>("/quoteHub");

app.MapRazorPages();

app.Run();
