using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Web.ViewModels.Account;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq; 
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Application.Services;

namespace OperaLearningSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly OperaDbContext _context;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<AccountController> logger,
            ICategoryService categoryService,
            IWebHostEnvironment webHostEnvironment,
            IEmailService emailService,
            OperaDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _categoryService = categoryService;
            _webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
            _context = context;

        }

        // GET: /Account/Login 登录
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login 登录
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return LocalRedirect(returnUrl);
                }
                ModelState.AddModelError(string.Empty, "登录失败，请检查邮箱或密码。");
            }
            return View(model);
        }

        /// <summary>无权限访问资源时由 Identity 重定向至此（需存在对应视图，否则会 404）</summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // GET: /Account/Register 注册
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Index", "Home");
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register 注册
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 创建新用户
                var user = new User { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    // 注册成功后，跳转到完善资料页面
                    return RedirectToAction("CompleteProfile", "Account");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/CompleteProfile 完善资料
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CompleteProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"无法加载用户 ID 为 '{_userManager.GetUserId(User)}' 的用户。");
            }

            var viewModel = new CompleteProfileViewModel
            {
                Email = user.Email,
                // 初始化列表
                HobbyOptions = new List<HobbyOption>()
            };

            // 获取所有剧种分类，生成选项
            var categories = await _categoryService.GetAllAsync();
            foreach (var category in categories)
            {
                viewModel.HobbyOptions.Add(new HobbyOption
                {
                    CategoryId = category.Id,
                    Name = category.Name,
                    IsSelected = false // 首次完善资料，默认都不选中
                });
            }

            viewModel.ProvinceOptions = GetProvinceSelectList();
            return View(viewModel);
        }

        // POST: /Account/CompleteProfile 完善资料
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"无法加载用户 ID 为 '{_userManager.GetUserId(User)}' 的用户。");
            }

            if (!ModelState.IsValid)
            {
                // 验证失败，重新加载选项数据
                var categories = await _categoryService.GetAllAsync();
                model.HobbyOptions = new List<HobbyOption>();
                foreach (var category in categories)
                {
                    // 注意：这里简单重新填充，如果想体验更好，应该根据 model 里的值设置 IsSelected
                    model.HobbyOptions.Add(new HobbyOption { CategoryId = category.Id, Name = category.Name });
                }
                model.ProvinceOptions = GetProvinceSelectList();
                return View(model);
            }

            // --- 更新用户信息 ---
            user.Nickname = model.Nickname;
            user.BirthDate = model.BirthDate;
            user.Gender = model.Gender;
            user.Province = model.SelectedProvince;
            user.Bio = model.Bio;

            // --- 关键修复：处理头像上传 ---
            // 增加 Length > 0 检查，确保文件有效
            if (model.AvatarImage != null && model.AvatarImage.Length > 0)
            {
                // 使用 Path.GetFileName 过滤掉客户端可能传来的路径字符，只保留文件名
                string safeFileName = Path.GetFileName(model.AvatarImage.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + safeFileName;

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "avatars");
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarImage.CopyToAsync(fileStream);
                }

                // 更新用户的头像路径
                user.AvatarUrl = "/images/avatars/" + uniqueFileName;
            }

            // --- 关键修复：处理戏曲爱好保存 ---
            if (model.HobbyOptions != null)
            {
                var selectedHobbies = model.HobbyOptions
                    .Where(h => h.IsSelected)
                    .Select(h => h.Name)
                    .ToList();

                user.Hobbies = string.Join(",", selectedHobbies);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // [重要] 刷新登录凭证，确保最新的 AvatarUrl 和其他信息立即生效
                await _signInManager.RefreshSignInAsync(user);

                // 设置成功消息，首页需要读取这个 TempData
                TempData["SuccessMessage"] = "恭喜！您的资料已完善，欢迎入驻梨园！";

                // 跳转回首页
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // 失败回退逻辑
            var categoriesDb = await _categoryService.GetAllAsync();
            model.HobbyOptions = new List<HobbyOption>();
            foreach (var category in categoriesDb)
            {
                model.HobbyOptions.Add(new HobbyOption { CategoryId = category.Id, Name = category.Name });
            }
            model.ProvinceOptions = GetProvinceSelectList();
            return View(model);
        }

        // GET: /Account/EditProfile 编辑资料
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"无法加载用户 ID 为 '{_userManager.GetUserId(User)}' 的用户。");
            }

            // 1. 创建 ViewModel 并填充基础信息
            var viewModel = new CompleteProfileViewModel
            {
                Email = user.Email,
                Nickname = user.Nickname,
                BirthDate = user.BirthDate,
                Gender = user.Gender ?? "保密",
                SelectedProvince = user.Province,
                Bio = user.Bio,
                ExistingAvatarUrl = user.AvatarUrl // 填充已有头像URL
            };

            // 2. 解析已保存的爱好字符串 (例如 "京剧,昆曲")
            var userHobbiesList = new List<string>();
            if (!string.IsNullOrEmpty(user.Hobbies))
            {
                userHobbiesList = user.Hobbies.Split(',').ToList();
            }

            // 3. 填充爱好选项列表，并设置 IsSelected
            var categories = await _categoryService.GetAllAsync();
            viewModel.HobbyOptions = new List<HobbyOption>();
            foreach (var category in categories)
            {
                viewModel.HobbyOptions.Add(new HobbyOption
                {
                    CategoryId = category.Id,
                    Name = category.Name,
                    // 如果用户的爱好列表中包含这个分类名，则设为选中
                    IsSelected = userHobbiesList.Contains(category.Name)
                });
            }

            viewModel.ProvinceOptions = GetProvinceSelectList();

            // 复用 CompleteProfile 视图
            return View("CompleteProfile", viewModel);
        }

        // POST: /Account/EditProfile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(CompleteProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"无法加载用户 ID 为 '{_userManager.GetUserId(User)}' 的用户。");
            }

            if (model.Email != user.Email)
            {
                ModelState.AddModelError("Email", "注册邮箱不能被修改。");
            }

            if (!ModelState.IsValid)
            {
                // 验证失败重载数据
                var categories = await _categoryService.GetAllAsync();
                model.HobbyOptions = new List<HobbyOption>();
                foreach (var category in categories)
                {
                    model.HobbyOptions.Add(new HobbyOption { CategoryId = category.Id, Name = category.Name });
                }
                model.ProvinceOptions = GetProvinceSelectList();
                return View("CompleteProfile", model);
            }

            // 更新字段
            user.Nickname = model.Nickname;
            user.BirthDate = model.BirthDate;
            user.Gender = model.Gender;
            user.Province = model.SelectedProvince;
            user.Bio = model.Bio;

            // --- 关键修复：处理头像上传 ---
            if (model.AvatarImage != null && model.AvatarImage.Length > 0)
            {
                string safeFileName = Path.GetFileName(model.AvatarImage.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + safeFileName;

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "avatars");
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarImage.CopyToAsync(fileStream);
                }
                user.AvatarUrl = "/images/avatars/" + uniqueFileName;
            }

            // --- 关键修复：保存爱好 ---
            if (model.HobbyOptions != null)
            {
                var selectedHobbies = model.HobbyOptions
                    .Where(h => h.IsSelected)
                    .Select(h => h.Name)
                    .ToList();
                user.Hobbies = string.Join(",", selectedHobbies);
            }
            // --------------------------

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // [重要] 刷新登录凭证，确保最新的 AvatarUrl 和其他信息立即生效
                await _signInManager.RefreshSignInAsync(user);

                // 设置成功消息
                TempData["SuccessMessage"] = "您的个人资料已成功更新！";

                // 修改此处：确保跳转回首页
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // 失败回退逻辑
            var categoriesDb = await _categoryService.GetAllAsync();
            model.HobbyOptions = new List<HobbyOption>();
            foreach (var category in categoriesDb)
            {
                model.HobbyOptions.Add(new HobbyOption { CategoryId = category.Id, Name = category.Name });
            }
            model.ProvinceOptions = GetProvinceSelectList();
            return View("CompleteProfile", model);
        }

        // GET: /Account/UserCenter
        [Authorize]
        public async Task<IActionResult> UserCenter()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var viewModel = new UserCenterViewModel
            {
                User = user,
                Favorites = new List<UserContentItem>(),
                Likes = new List<UserContentItem>()
            };

            // 1. 获取收藏数据 (Favorite)
            var favorites = await _context.Favorites
                .Where(f => f.UserId == user.Id)
                .Include(f => f.Play)
                .Include(f => f.Master)
                .Include(f => f.Course)
                .ToListAsync();

            foreach (var fav in favorites)
            {
                if (fav.Play != null)
                {
                    viewModel.Favorites.Add(new UserContentItem
                    {
                        Id = fav.Play.Id,
                        Title = fav.Play.Title,
                        ImageUrl = fav.Play.ImageUrl,
                        ContentType = "剧目",
                        ControllerName = "Play"
                    });
                }
                else if (fav.Master != null)
                {
                    viewModel.Favorites.Add(new UserContentItem
                    {
                        Id = fav.Master.Id,
                        Title = fav.Master.Name, // 注意：Master用的是 Name
                        ImageUrl = fav.Master.ImageUrl,
                        ContentType = "名家",
                        ControllerName = "Master"
                    });
                }
                else if (fav.Course != null)
                {
                    viewModel.Favorites.Add(new UserContentItem
                    {
                        Id = fav.Course.Id,
                        Title = fav.Course.Name, // 注意：Course用的是 Name
                        ImageUrl = fav.Course.ImageUrl,
                        ContentType = "课程",
                        ControllerName = "Course"
                    });
                }
            }

            // 2. 获取点赞数据 (Like)
            var likes = await _context.Likes
                .Where(l => l.UserId == user.Id)
                .Include(l => l.Play)
                .Include(l => l.Master)
                .Include(l => l.Course)
                .ToListAsync();

            foreach (var like in likes)
            {
                if (like.Play != null)
                {
                    viewModel.Likes.Add(new UserContentItem
                    {
                        Id = like.Play.Id,
                        Title = like.Play.Title,
                        ImageUrl = like.Play.ImageUrl,
                        ContentType = "剧目",
                        ControllerName = "Play"
                    });
                }
                else if (like.Master != null)
                {
                    viewModel.Likes.Add(new UserContentItem
                    {
                        Id = like.Master.Id,
                        Title = like.Master.Name,
                        ImageUrl = like.Master.ImageUrl,
                        ContentType = "名家",
                        ControllerName = "Master"
                    });
                }
                else if (like.Course != null)
                {
                    viewModel.Likes.Add(new UserContentItem
                    {
                        Id = like.Course.Id,
                        Title = like.Course.Name,
                        ImageUrl = like.Course.ImageUrl,
                        ContentType = "课程",
                        ControllerName = "Course"
                    });
                }
            }

            viewModel.User = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
            ViewBag.Badges = await _context.UserBadges.AsNoTracking()
                .Where(b => b.UserId == user.Id).OrderByDescending(b => b.EarnedAt).ToListAsync();
            ViewBag.CheckInCount = await _context.CommunityPosts
                .CountAsync(p => p.AuthorId == user.Id && p.PostKind == 1);

            var profile = await _context.UserLearningProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            ViewBag.AbilityEstimate = profile?.AbilityEstimate ?? 1.0;
            ViewBag.CorrectStreak = profile?.CorrectStreak ?? 0;

            var quizAttempts = await _context.UserCourseQuizAttempts.AsNoTracking()
                .Where(a => a.UserId == user.Id)
                .Include(a => a.Course)
                .OrderByDescending(a => a.FinishedAt)
                .Take(80)
                .ToListAsync();
            viewModel.CourseQuizHistory = quizAttempts.Select(a => new CourseQuizHistoryRow
            {
                CourseId = a.CourseId,
                CourseName = a.Course?.Name ?? "（课程已删除）",
                CorrectCount = a.CorrectCount,
                TotalCount = a.TotalCount,
                AccuracyPercent = a.TotalCount > 0 ? Math.Round(100.0 * a.CorrectCount / a.TotalCount, 1) : 0,
                FinishedAt = a.FinishedAt
            }).ToList();

            var bookmarkPostIds = await _context.Likes.AsNoTracking()
                .Where(l => l.UserId == user.Id && l.ReactionKind == 3 && l.CommunityPostId != null)
                .Select(l => l.CommunityPostId!.Value)
                .Distinct()
                .ToListAsync();
            var bmPosts = await _context.CommunityPosts.AsNoTracking()
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Where(p => bookmarkPostIds.Contains(p.Id))
                .OrderByDescending(p => p.CreatedTime)
                .Take(200)
                .ToListAsync();
            foreach (var p in bmPosts)
            {
                var text = p.Content ?? "";
                if (text.Length > 180) text = text.Substring(0, 180) + "…";
                viewModel.PostBookmarks.Add(new UserCommunityPostBookmarkItem
                {
                    PostId = p.Id,
                    Title = p.Title ?? "",
                    Excerpt = text,
                    AuthorDisplay = p.Author?.Nickname ?? p.Author?.UserName ?? "戏友",
                    CategoryName = p.Category?.Name,
                    CreatedTime = p.CreatedTime
                });
            }

            return View(viewModel);
        }

        // Account/ChangePassword
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound($"无法加载用户 ID '{_userManager.GetUserId(User)}'.");

            // Identity 自带的更改密码方法，极其安全，自动比对旧密码并加密新密码
            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // 修改成功后刷新登录状态
            await _signInManager.RefreshSignInAsync(user);
            TempData["SuccessMessage"] = "您的密码已成功修改。";
            return RedirectToAction("UserCenter"); // 修改完跳回个人中心
        }

        // Account/ForgotPassword 忘记密码
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return RedirectToAction("ForgotPasswordConfirmation");
                }

                // 1. 生成一次性的安全重置 Token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // 2. 生成回调地址（指向我们下一步要写的 ResetPassword 动作）
                var callbackUrl = Url.Action("ResetPassword", "Account", new { token, email = user.Email }, Request.Scheme);

                // 3. 发送真实的重置邮件
                var emailBody = $"<h3>畅音雅韵 - 密码重置</h3><p>请点击下方链接重置您的密码：</p><a href='{callbackUrl}'>点击这里重置密码</a>";
                await _emailService.SendEmailAsync(model.Email, "畅音雅韵 - 重置密码", emailBody);

                return RedirectToAction("ForgotPasswordConfirmation");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                ModelState.AddModelError("", "无效的密码重置令牌。");
            }
            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // 不要泄露用户不存在
                return RedirectToAction("ResetPasswordConfirmation");
            }

            // 验证 Token 并强制重置为新密码
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            // 提示用户“密码重置成功，请点击此处重新登录”
            return View();
        }


        // 辅助方法：获取省份列表
        private List<SelectListItem> GetProvinceSelectList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "--- 请选择省份 ---" },
                new SelectListItem { Value = "北京市", Text = "北京市" },
                new SelectListItem { Value = "上海市", Text = "上海市" },
                new SelectListItem { Value = "天津市", Text = "天津市" },
                new SelectListItem { Value = "重庆市", Text = "重庆市" },
                new SelectListItem { Value = "河北省", Text = "河北省" },
                new SelectListItem { Value = "山西省", Text = "山西省" },
                new SelectListItem { Value = "辽宁省", Text = "辽宁省" },
                new SelectListItem { Value = "吉林省", Text = "吉林省" },
                new SelectListItem { Value = "黑龙江省", Text = "黑龙江省" },
                new SelectListItem { Value = "江苏省", Text = "江苏省" },
                new SelectListItem { Value = "浙江省", Text = "浙江省" },
                new SelectListItem { Value = "安徽省", Text = "安徽省" },
                new SelectListItem { Value = "福建省", Text = "福建省" },
                new SelectListItem { Value = "江西省", Text = "江西省" },
                new SelectListItem { Value = "山东省", Text = "山东省" },
                new SelectListItem { Value = "河南省", Text = "河南省" },
                new SelectListItem { Value = "湖北省", Text = "湖北省" },
                new SelectListItem { Value = "湖南省", Text = "湖南省" },
                new SelectListItem { Value = "广东省", Text = "广东省" },
                new SelectListItem { Value = "海南省", Text = "海南省" },
                new SelectListItem { Value = "四川省", Text = "四川省" },
                new SelectListItem { Value = "贵州省", Text = "贵州省" },
                new SelectListItem { Value = "云南省", Text = "云南省" },
                new SelectListItem { Value = "陕西省", Text = "陕西省" },
                new SelectListItem { Value = "甘肃省", Text = "甘肃省" },
                new SelectListItem { Value = "青海省", Text = "青海省" },
                new SelectListItem { Value = "台湾省", Text = "台湾省" },
                new SelectListItem { Value = "内蒙古自治区", Text = "内蒙古自治区" },
                new SelectListItem { Value = "广西壮族自治区", Text = "广西壮族自治区" },
                new SelectListItem { Value = "西藏自治区", Text = "西藏自治区" },
                new SelectListItem { Value = "宁夏回族自治区", Text = "宁夏回族自治区" },
                new SelectListItem { Value = "新疆维吾尔自治区", Text = "新疆维吾尔自治区" },
                new SelectListItem { Value = "香港特别行政区", Text = "香港特别行政区" },
                new SelectListItem { Value = "澳门特别行政区", Text = "澳门特别行政区" }
            };
        }
    }
}