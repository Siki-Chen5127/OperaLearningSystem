using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OperaLearningSystem.Web.Areas.Admin.Controllers
{
    [Area("Admin")] // 声明此控制器属于 Admin 区域
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class BaseAdminController : Controller
    {
        // 所有后台控制器都继承自这个基类，从而自动获得权限保护
    }
}