using System;
using System.Threading;
using System.Threading.Tasks;

namespace SamplePlugin
{
    public class MyServiceC
    {
        private bool _stopping;
        private Task _backgroundTask;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("MyServiceC is starting.");
            _backgroundTask = BackgroundTask();
            return Task.CompletedTask;
        }

        private async Task BackgroundTask()
        {
            while (!_stopping)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                Console.WriteLine("MyServiceC is doing background work.");
            }

            Console.WriteLine("MyServiceC background task is stopping.");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("MyServiceC is stopping.");
            _stopping = true;
            if (_backgroundTask != null)
            {
                // TODO: cancellation
                await _backgroundTask;
            }
        }
    }
}
