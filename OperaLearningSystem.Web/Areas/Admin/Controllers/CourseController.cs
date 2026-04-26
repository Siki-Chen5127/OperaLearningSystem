namespace OperaLearningSystem.Web.Areas.Admin.Controllers
{
    using System.Text.Json;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.EntityFrameworkCore;
    using OperaLearningSystem.Core.Entities;
    using OperaLearningSystem.Core.Interfaces;
    using OperaLearningSystem.Infrastructure.Data;
    using OperaLearningSystem.Web.Areas.Admin.ViewModels;
    using OperaLearningSystem.Web.Services;

    public class CourseController : BaseAdminController
    {
        private readonly ICourseService _courseService;
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<User> _userManager;
        private readonly OperaDbContext _db;
        private readonly CourseQuizAiService _courseQuizAi;
        public CourseController(
            ICourseService courseService,
            ICategoryService categoryService,
            IWebHostEnvironment webHostEnvironment,
            UserManager<User> userManager,
            OperaDbContext db,
            CourseQuizAiService courseQuizAi)
        {
            _courseService = courseService;
            _categoryService = categoryService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _db = db;
            _courseQuizAi = courseQuizAi;
        }
        public async Task<IActionResult> Index(int pageNumber = 1, string? searchString = null, int? categoryId = null)
        {
            const int pageSize = 6;

            var pagedResult = await _courseService.GetPagedAsync(pageNumber, pageSize, searchString, categoryId);
            return View(pagedResult);
        }
        private async Task PopulateViewModelOptions(CourseEditViewModel viewModel)
        {
            var categories = await _categoryService.GetCategoriesForSelectListAsync();
            viewModel.CategoryOptions = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
        }
        private async Task PopulateViewModelOptions(CourseCreateViewModel viewModel)
        {
            var categories = await _categoryService.GetCategoriesForSelectListAsync();
            ViewBag.CategoryId = new SelectList(categories, "Id", "Name", viewModel.CategoryId);
        }
        public async Task<IActionResult> Create()
        {
            var viewModel = new CourseCreateViewModel();
            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CourseCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                bool isSuperAdmin = await _userManager.IsInRoleAsync(currentUser, "SuperAdmin");
                string uniqueFileName = await UploadFile(viewModel.ImageFile);
                Course newCourse = new Course
                {
                    Name = viewModel.Name,
                    Description = viewModel.Description,
                    VideoUrl = viewModel.VideoUrl,
                    IsFeatured = viewModel.IsFeatured,
                    CategoryId = viewModel.CategoryId,
                    ImageUrl = uniqueFileName,
                    SubmitterId = currentUser?.Id,
                    AuditStatus = isSuperAdmin ? 1 : 0
                };
                await _courseService.AddAsync(newCourse);
                TempData["SuccessMessage"] = isSuperAdmin
                    ? $"课程 “{newCourse.Name}” 发布成功！"
                    : $"课程 “{newCourse.Name}” 已呈递！请等待掌印审核。";
                return RedirectToAction(nameof(Index));
            }
            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var course = await _courseService.GetCourseDetailsByIdAsync(id.Value);
            if (course == null) return NotFound();
            var viewModel = new CourseEditViewModel
            {
                Id = course.Id,
                Name = course.Name,
                Description = course.Description,
                VideoUrl = course.VideoUrl,
                IsFeatured = course.IsFeatured,
                CategoryId = course.CategoryId,
                ExistingImageUrl = course.ImageUrl
            };
            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CourseEditViewModel viewModel)
        {
            if (id != viewModel.Id) return NotFound();

            ModelState.Remove("ImageFile");

            if (ModelState.IsValid)
            {
                var courseToUpdate = await _courseService.GetCourseDetailsByIdAsync(id);
                if (courseToUpdate == null) return NotFound();
                var currentUser = await _userManager.GetUserAsync(User);
                bool isSuperAdmin = await _userManager.IsInRoleAsync(currentUser, "SuperAdmin");
                if (viewModel.ImageFile != null)
                {
                    courseToUpdate.ImageUrl = await UploadFile(viewModel.ImageFile);
                }

                courseToUpdate.Name = viewModel.Name;
                courseToUpdate.Description = viewModel.Description;
                courseToUpdate.VideoUrl = viewModel.VideoUrl;
                courseToUpdate.IsFeatured = viewModel.IsFeatured;
                courseToUpdate.CategoryId = viewModel.CategoryId;
                if (!isSuperAdmin)
                {
                    courseToUpdate.AuditStatus = 0;
                    courseToUpdate.SubmitterId = currentUser?.Id;
                }
                await _courseService.UpdateAsync(courseToUpdate);
                TempData["SuccessMessage"] = isSuperAdmin
                        ? $"课程 “{courseToUpdate.Name}” 更新成功！"
                        : $"课程 “{courseToUpdate.Name}” 修改已提交，重新进入待审核状态！";                 return RedirectToAction(nameof(Index));
            }
            await PopulateViewModelOptions(viewModel);
            return View(viewModel);
        }

        /// <summary>
        /// 根据课程名称与简介调用大模型生成本课程专属单选题，并替换该课程旧有的 AI/机器题库。
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateQuizWithAi(int id, CancellationToken ct)
        {
            var course = await _courseService.GetCourseDetailsByIdAsync(id);
            if (course == null) return NotFound();
            if (string.IsNullOrWhiteSpace(course.Description))
            {
                TempData["ErrorMessage"] = "请先填写课程简介，再使用 AI 生成考卷。";
                return RedirectToAction(nameof(Edit), new { id });
            }

            try
            {
                var questions = await _courseQuizAi.GenerateForCourseAsync(
                    course.Id, course.Name, course.Description, questionCount: 5, ct);

                var old = await _db.QuizQuestions.Where(q => q.CourseId == id).ToListAsync(ct);
                _db.QuizQuestions.RemoveRange(old);
                _db.QuizQuestions.AddRange(questions);
                await _db.SaveChangesAsync(ct);

                TempData["SuccessMessage"] =
                    $"已生成 {questions.Count} 道考题，请在「专属题库」中核对与修改后再让学员开考。";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "AI 生成失败：" + ex.Message;
            }

            return RedirectToAction(nameof(QuizQuestions), new { id });
        }

        /// <summary>本课程专属题库列表（可看、改、删、手动新增）。</summary>
        public async Task<IActionResult> QuizQuestions(int id, CancellationToken ct)
        {
            var course = await _courseService.GetCourseDetailsByIdAsync(id);
            if (course == null) return NotFound();

            var list = await _db.QuizQuestions.AsNoTracking()
                .Where(q => q.CourseId == id)
                .OrderBy(q => q.Id)
                .ToListAsync(ct);
            var rows = list.Select(q => new CourseQuizQuestionRow
            {
                Id = q.Id,
                PromptPreview = q.Prompt.Length > 80 ? q.Prompt.Substring(0, 80) + "…" : q.Prompt,
                CorrectIndex = q.CorrectIndex,
                Tags = q.Tags
            }).ToList();

            var vm = new CourseQuizQuestionsPageViewModel
            {
                CourseId = id,
                CourseName = course.Name,
                Questions = rows
            };
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> QuizQuestionEdit(int courseId, int? id, CancellationToken ct)
        {
            var course = await _courseService.GetCourseDetailsByIdAsync(courseId);
            if (course == null) return NotFound();

            var vm = new CourseQuizQuestionEditViewModel
            {
                CourseId = courseId,
                CourseName = course.Name
            };

            if (id is > 0)
            {
                var q = await _db.QuizQuestions.FirstOrDefaultAsync(x => x.Id == id && x.CourseId == courseId, ct);
                if (q == null) return NotFound();

                vm.Id = q.Id;
                vm.Prompt = q.Prompt;
                vm.CorrectIndex = q.CorrectIndex;
                vm.Explanation = q.Explanation;
                vm.QuestionType = q.QuestionType;
                vm.Difficulty = q.Difficulty;
                string[] opts;
                try { opts = JsonSerializer.Deserialize<string[]>(q.OptionsJson) ?? Array.Empty<string>(); }
                catch { opts = Array.Empty<string>(); }
                vm.Option0 = opts.Length > 0 ? opts[0] : "";
                vm.Option1 = opts.Length > 1 ? opts[1] : "";
                vm.Option2 = opts.Length > 2 ? opts[2] : "";
                vm.Option3 = opts.Length > 3 ? opts[3] : "";
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuizQuestionEdit(CourseQuizQuestionEditViewModel vm, CancellationToken ct)
        {
            var course = await _courseService.GetCourseDetailsByIdAsync(vm.CourseId);
            if (course == null) return NotFound();

            vm.CourseName = course.Name;

            if (string.IsNullOrWhiteSpace(vm.Option0) || string.IsNullOrWhiteSpace(vm.Option1)
                || string.IsNullOrWhiteSpace(vm.Option2) || string.IsNullOrWhiteSpace(vm.Option3))
                ModelState.AddModelError(string.Empty, "四个选项均需填写。");

            if (!ModelState.IsValid)
                return View(vm);

            var optsJson = JsonSerializer.Serialize(new[]
            {
                vm.Option0.Trim(), vm.Option1.Trim(), vm.Option2.Trim(), vm.Option3.Trim()
            });

            if (vm.Id <= 0)
            {
                _db.QuizQuestions.Add(new QuizQuestion
                {
                    CourseId = vm.CourseId,
                    Prompt = vm.Prompt.Trim(),
                    OptionsJson = optsJson,
                    CorrectIndex = vm.CorrectIndex,
                    Explanation = string.IsNullOrWhiteSpace(vm.Explanation) ? null : vm.Explanation.Trim(),
                    QuestionType = vm.QuestionType,
                    Difficulty = vm.Difficulty,
                    Tags = "manual,course"
                });
                TempData["SuccessMessage"] = "已新增题目。";
            }
            else
            {
                var q = await _db.QuizQuestions.FirstOrDefaultAsync(x => x.Id == vm.Id && x.CourseId == vm.CourseId, ct);
                if (q == null) return NotFound();
                q.Prompt = vm.Prompt.Trim();
                q.OptionsJson = optsJson;
                q.CorrectIndex = vm.CorrectIndex;
                q.Explanation = string.IsNullOrWhiteSpace(vm.Explanation) ? null : vm.Explanation.Trim();
                q.QuestionType = vm.QuestionType;
                q.Difficulty = vm.Difficulty;
                TempData["SuccessMessage"] = "题目已保存。";
            }

            await _db.SaveChangesAsync(ct);
            return RedirectToAction(nameof(QuizQuestions), new { id = vm.CourseId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuizQuestionDelete(int id, int courseId, CancellationToken ct)
        {
            var q = await _db.QuizQuestions.FirstOrDefaultAsync(x => x.Id == id && x.CourseId == courseId, ct);
            if (q == null) return NotFound();
            _db.QuizQuestions.Remove(q);
            await _db.SaveChangesAsync(ct);
            TempData["SuccessMessage"] = "已删除该题。";
            return RedirectToAction(nameof(QuizQuestions), new { id = courseId });
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var course = await _courseService.GetCourseDetailsByIdAsync(id.Value);
            if (course == null) return NotFound();
            return Json(new { id = course.Id, name = course.Name });
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _courseService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
        private async Task<string> UploadFile(IFormFile imageFile)
        {
            if (imageFile == null) return null;
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "courses");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }
            return $"/images/courses/{uniqueFileName}";
        }
    }
}