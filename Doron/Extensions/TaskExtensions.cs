using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doron.Extensions
{
    internal static class TaskExtensions
    {
        public static async Task<TResult> RunWithTimeout<TResult>(this Task<TResult> task, int timeout)
        {
            Task timeoutTask = Task.Delay(timeout);

            if (await Task.WhenAny(task, timeoutTask) == timeoutTask)
                throw new TimeoutException();

            return task.Result;
        }
        public static async Task RunWithTimeout(this Task task, int timeout)
        {
            Task timeoutTask = Task.Delay(timeout);

            if (await Task.WhenAny(task, timeoutTask) == timeoutTask)
                throw new TimeoutException();
        }
    }
}
