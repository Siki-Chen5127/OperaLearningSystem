using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaLearningSystem.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Controllers
{
    public class QuoteBoardController : Controller
    {
        private readonly OperaDbContext _context;

        public QuoteBoardController(OperaDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? from = null, string? yueshilouReturn = null)
        {
            ViewBag.ReturnToYaji = string.Equals(from, "yaji", StringComparison.OrdinalIgnoreCase);
            ViewBag.ReturnToYueshilou = string.Equals(yueshilouReturn, "1", StringComparison.Ordinal);

            var recentQuotes = await _context.OperaQuotes
                .AsNoTracking()
                .Include(q => q.Author) // 加载作者信息
                .OrderByDescending(q => q.CreatedTime) // 按时间倒序
                .Take(50) // 取最近50条
                .ToListAsync();

            recentQuotes.Reverse();

            // 3. 将历史数据传递给视图
            return View(recentQuotes);
        }
    }
}