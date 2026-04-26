using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OperaLearningSystem.Core.Interfaces;
using OperaLearningSystem.Application.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OperaLearningSystem.Web.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class TestApiController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public TestApiController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet("categoryplaycounts")] // 为了URL规范，改为全小写
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<ActionResult<IEnumerable<ChartDataDto>>> GetCategoryPlayCounts()
        {
            var categories = await _categoryService.GetAllAsync();

            var chartData = categories.Select(c => new ChartDataDto
            {
                Name = c.Name,
                Value = c.Plays?.Count ?? 0
            }).ToList();

            return Ok(chartData);
        }
    }
}