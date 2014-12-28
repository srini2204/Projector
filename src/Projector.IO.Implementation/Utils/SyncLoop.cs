using Projector.Collections;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Projector.IO.Implementation.Utils
{
    public class SyncLoop : ISyncLoop
    {
        private IAsyncCollection<Action> _actionQueue;

        public SyncLoop()
        {
            _actionQueue = new AsyncCollection<Action>(new ConcurrentQueue<Action>());
        }

        public async Task StartProcessActions(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var action = await _actionQueue.TakeAsync();

                action();
            }
        }

        public async Task Run(Action action)
        {
            var complectionSource = new ReusableTaskCompletionSource<int>();
            _actionQueue.Add(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        complectionSource.SetException(e);
                    }
                    complectionSource.SetResult(0);
                });
            await complectionSource;
        }
    }
}
