using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SamplePlugin
{
    public class MyServiceD
    {
        private bool _stopping;
        private Task _backgroundTask;
        private ILogger logger;

        public Task StartAsync(CancellationToken cancellationToken, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(LogLevel.Debug);
            this.logger = loggerFactory.CreateLogger<MyServiceD>();
            this.logger.LogInformation("MyServiceD is starting.");
            _backgroundTask = BackgroundTask();
            return Task.CompletedTask;
        }

        private async Task BackgroundTask()
        {
            while (!_stopping)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                logger.LogInformation("MyServiceD is doing background work.");
            }

            logger.LogInformation("MyServiceD background task is stopping.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("MyServiceD is stopping.");
            _stopping = true;
            if (_backgroundTask != null)
            {
                // TODO: cancellation
                await _backgroundTask;
            }
        }
    }
}