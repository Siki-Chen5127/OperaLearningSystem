using Microsoft.AspNetCore.Mvc;
using OperaLearningSystem.Core.Interfaces; // ?????????????
using System.Diagnostics;
using OperaLearningSystem.Web.Models; // ???? ErrorViewModel
using OperaLearningSystem.Web.ViewModels.Home;

namespace OperaLearningSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPlayService _playService; // ??? PlayService

        // ?????????
        public HomeController(ILogger<HomeController> logger, IPlayService playService)
        {
            _logger = logger;
            _playService = playService;
        }

        public async Task<IActionResult> Index()
        {
            // ��?????????????????????????????????????��?????
            // 1. ????????? (???????????????5??)
            var recommendedPlays = await _playService.GetRecommendedAsync(5);

            // 2. ????????? (?????????????10??)
            var lyrics = await _playService.GetRandomLyricsAsync(10);

            // 3. ??? ViewModel
            var viewModel = new HomeViewModel
            {
                RecommendedPlays = recommendedPlays,
                Lyrics = lyrics
            };

            return View(viewModel);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}