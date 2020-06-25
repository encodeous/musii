using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace musii.Utilities
{
    static class TaskUtils
    {
        public static void Forget(Func<Task> t)
        {
            Task.Run(t).ConfigureAwait(false);
        }

        public static void Recur(Action action, TimeSpan delay, CancellationToken token)
        {
            Task.Run(async () => {
                while (!token.IsCancellationRequested)
                {
                    DateTime startTime = DateTime.Now;

                    action();

                    var execTime = DateTime.Now - startTime;

                    if (execTime < delay)
                    {
                        await Task.Delay(delay - execTime, token).ConfigureAwait(false);
                    }
                }
            }, token);
        }

    }
}
