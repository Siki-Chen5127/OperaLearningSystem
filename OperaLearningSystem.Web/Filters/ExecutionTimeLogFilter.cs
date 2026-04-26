using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics; //引入这个命名空间来使用 Stopwatch

namespace OperaLearningSystem.Web.Filters
{
    public class ExecutionTimeLogFilter : IAsyncActionFilter
    {
        private readonly ILogger<ExecutionTimeLogFilter> _logger;

        public ExecutionTimeLogFilter(ILogger<ExecutionTimeLogFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var stopwatch = Stopwatch.StartNew();
            var controllerName = context.Controller.GetType().Name;
            var actionName = context.ActionDescriptor.DisplayName;

            _logger.LogInformation("==> Executing action: {ActionName} on controller {ControllerName}", actionName, controllerName);

            var resultContext = await next();

            stopwatch.Stop();
            var timeElapsed = stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("<== Finished action: {ActionName} on controller {ControllerName}. Duration: {TimeElapsed}ms", actionName, controllerName, timeElapsed);

            if (resultContext.Exception != null)
            {
                _logger.LogError(resultContext.Exception, "An unhandled exception occurred during the execution of {ActionName}", actionName);
            }
        }
    }
}