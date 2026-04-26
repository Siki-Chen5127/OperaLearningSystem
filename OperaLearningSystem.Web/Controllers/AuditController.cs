using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Core.Entities;
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Web.ViewModels.Account; 
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AuditController : Controller
    {
        private readonly OperaDbContext _context;
        private readonly UserManager<User> _userManager;

        public AuditController(OperaDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //1.页面主页
        public async Task<IActionResult> Index()
        {
            // A. 查人员拜帖
            var pendingApplications = await _context.AdminApplications
                .Include(a => a.User)
                .Where(a => a.Status == 0)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync();
            // B. 查待审剧目
            var pendingPlays = await _context.Plays
                .Include(p => p.Submitter)
                .Include(p => p.Category)
                .Where(p => p.AuditStatus == 0)
                .OrderByDescending(p => p.Id)
                .ToListAsync();
            //    查审核名家
            var pendingMasters = await _context.Masters
                .Include(m => m.Submitter)
                .Include(m => m.Category)
                .Where(m => m.AuditStatus == 0)
                .OrderByDescending(m => m.Id)
                .ToListAsync();
            //    审核剧种
            var pendingCategories = await _context.Categories
                .Include(c => c.Submitter)
                .Where(c => c.AuditStatus == 0)
                .OrderByDescending(c => c.Id)
                .ToListAsync();
            //    审核课程
            var pendingCourses = await _context.Courses
                    .Include(c => c.Submitter)
                    .Include(c => c.Category)
                    .Where(c => c.AuditStatus == 0)
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();
            // C. 整合送给前台
            var viewModel = new AuditIndexViewModel
            {
                PendingApplications = pendingApplications,
                PendingPlays = pendingPlays,
                PendingMasters = pendingMasters,
                PendingCategories = pendingCategories,
                PendingCourses = pendingCourses
            };
            return View(viewModel);
        }

        // 2.审批
        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var application = await _context.AdminApplications.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
            if (application == null) return NotFound();

            application.Status = 1;
            application.ProcessedAt = DateTime.Now;

            if (!await _userManager.IsInRoleAsync(application.User, "Admin"))
            {
                await _userManager.AddToRoleAsync(application.User, "Admin");
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"已通过审核 {application.User.Nickname ?? application.User.Email} 成为管理员！";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string rejectReason)
        {
            var application = await _context.AdminApplications.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id);
            if (application == null) return NotFound();

            application.Status = 2;
            application.RejectReason = string.IsNullOrWhiteSpace(rejectReason) ? "资历尚浅，望继续沉淀。" : rejectReason;
            application.ProcessedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"已驳回 {application.User.Nickname ?? application.User.Email} 的申请。";
            return RedirectToAction(nameof(Index));
        }

        // 3.1剧目审核
        [HttpPost]
        public async Task<IActionResult> ApprovePlay(int id)
        {
            var play = await _context.Plays.FindAsync(id);
            if (play == null) return NotFound();

            play.AuditStatus = 1; // 1 = 审核通过，正式发布！
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"剧目《{play.Title}》已放行，正式登台！";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> RejectPlay(int id)
        {
            var play = await _context.Plays.FindAsync(id);
            if (play == null) return NotFound();

            play.AuditStatus = 2; // 2 = 审核驳回
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"剧目《{play.Title}》已被驳回。";
            return RedirectToAction(nameof(Index));
        }

        //3.2名家审核
        [HttpPost]
        public async Task<IActionResult> ApproveMaster(int id)
        {
            var master = await _context.Masters.FindAsync(id);
            if (master == null) return NotFound();
            master.AuditStatus = 1;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"名家【{master.Name}】已赐予放行！";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> RejectMaster(int id)
        {
            var master = await _context.Masters.FindAsync(id);
            if (master == null) return NotFound();
            master.AuditStatus = 2;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"名家【{master.Name}】已被驳回。";
            return RedirectToAction(nameof(Index));
        }

        //3.3剧种审核
        [HttpPost]
        public async Task<IActionResult> ApproveCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            category.AuditStatus = 1;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"剧种【{category.Name}】已赐予放行！";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> RejectCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            category.AuditStatus = 2;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"剧种【{category.Name}】已被驳回。";
            return RedirectToAction(nameof(Index));
        }

        //3.4课程审核
        [HttpPost]
        public async Task<IActionResult> ApproveCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();
            course.AuditStatus = 1;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"课程【{course.Name}】已赐予放行！";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> RejectCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();
            course.AuditStatus = 2;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"课程【{course.Name}】已被驳回。";
            return RedirectToAction(nameof(Index));
        }
    }
}