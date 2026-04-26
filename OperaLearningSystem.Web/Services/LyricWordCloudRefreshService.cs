using Microsoft.Extensions.Caching.Memory;

namespace OperaLearningSystem.Web.Services;

/// <summary>定期失效词云缓存，促使下次请求重新扫描评论。</summary>
public class LyricWordCloudRefreshService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public LyricWordCloudRefreshService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(25), stoppingToken);
            using var scope = _scopeFactory.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            for (var k = 0; k <= 2; k++)
                cache.Remove($"community_word_cloud_v5_kind_{k}");
        }
    }
}
