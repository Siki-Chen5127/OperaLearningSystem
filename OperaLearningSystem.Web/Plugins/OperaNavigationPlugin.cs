using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Linq;
using Microsoft.EntityFrameworkCore; // 添加 EF Core 引用
using OperaLearningSystem.Infrastructure.Data;
using OperaLearningSystem.Core.Entities; // 引用实体

namespace OperaLearningSystem.Web.Plugins
{
    public class OperaNavigationPlugin
    {
        private readonly OperaDbContext _context;
        public OperaNavigationPlugin(OperaDbContext context)
        {
            _context = context;
        }

        [KernelFunction, Description("根据用户的意图跳转到网站的具体板块页面。例如用户说“去茶馆”、“想看课程”时调用。")]
        public string NavigateToPage(
            [Description("目标页面的关键词，如：首页, 剧目, 名家, 剧种, 课程, 社区, 戏词")] string pageKey)
        {
            string url = "/";
            string targetName = "首页";

            if (pageKey.Contains("首页") || pageKey.Contains("大厅"))
            {
                url = "/Home/Index";
                targetName = "大厅";
            }
            else if (pageKey.Contains("剧目") || pageKey.Contains("看戏"))
            {
                url = "/Play/Index";
                targetName = "品鉴剧目画廊";
            }
            else if (pageKey.Contains("名家") || pageKey.Contains("大师") || pageKey.Contains("演员"))
            {
                url = "/Master/Index";
                targetName = "名家风采列传";
            }
            else if (pageKey.Contains("剧种") || pageKey.Contains("地图") || pageKey.Contains("方志"))
            {
                url = "/Category/Index";
                targetName = "剧种方志";
            }
            else if (pageKey.Contains("课程") || pageKey.Contains("学习") || pageKey.Contains("私塾"))
            {
                url = "/Course/Index";
                targetName = "梨园私塾";
            }
            else if (pageKey.Contains("社区") || pageKey.Contains("论坛") || pageKey.Contains("茶馆"))
            {
                url = "/Community/Index";
                targetName = "梨园茶馆";
            }
            else if (pageKey.Contains("戏词") || pageKey.Contains("弹幕") || pageKey.Contains("心灯"))
            {
                url = "/QuoteBoard/Index";
                targetName = "梨园心灯";
            }
            else if (pageKey.Contains("个人") || pageKey.Contains("我") || pageKey.Contains("收藏"))
            {
                url = "/Account/UserCenter";
                targetName = "个人中心";
            }

            return $"COMMAND:REDIRECT|{url}";
        }

        [KernelFunction, Description("在数据库中搜索戏曲剧目。当用户想找具体的戏（如《牡丹亭》）时调用。")]
        public string SearchPlays(
            [Description("剧目名称关键词")] string keyword)
        {
            var plays = _context.Plays
                .AsNoTracking()
                .Where(p => p.Title.Contains(keyword))
                .Select(p => new { p.Title, p.Id })
                .Take(3)
                .ToList();

            if (!plays.Any())
            {
                return $"抱歉，小梨在藏书阁里没翻到关于“{keyword}”的剧目。您可以去【品鉴剧目】板块再仔细找找。";
            }

            if (plays.Count == 1)
            {
                return $"COMMAND:REDIRECT|/Play/Details/{plays[0].Id}";
            }

            var resultStr = string.Join("、", plays.Select(p => $"《{p.Title}》"));
            return $"为您找到了以下剧目：{resultStr}。请问您具体想看哪一部？";
        }

        [KernelFunction, Description("在数据库中搜索戏曲名家。当用户想了解某位大师（如梅兰芳）时调用。")]
        public string SearchMasters(
            [Description("名家姓名关键词")] string name)
        {
            var master = _context.Masters
                .AsNoTracking()
                .FirstOrDefault(m => m.Name.Contains(name));

            if (master == null)
            {
                return $"资料库里暂时没有收录这位名家。您可以去【名家风采】板块查阅完整名录。";
            }

            return $"COMMAND:REDIRECT|/Master/Details/{master.Id}";
        }

        [KernelFunction, Description("介绍本网站的特色功能和设计亮点。")]
        public string GetWebsiteHighlights()
        {
            return "【畅音雅韵】是传统文化与现代技术的结晶，我有几处得意之作：\n" +
                   "1. 🏮 **梨园心灯**：在戏词坊，每一句戏词都化作一盏孔明灯，承载着大家的祈福缓缓升空。\n" +
                   "2. 📜 **方志长卷**：探索剧种板块采用了交互式竹简目录，右侧是全景视窗，移步换景。\n" +
                   "3. 🏫 **梨园私塾**：在线课程被设计成了案头线装书，书签还会随风摆动呢。\n" +
                   "4. 🖼️ **沉浸画廊**：剧目海报采用了自由比例，配合水墨遮罩，极具东方美学。\n" +
                   "我是您的书童小梨，带您领略这方寸舞台间的天地大美！";
        }
    }
}