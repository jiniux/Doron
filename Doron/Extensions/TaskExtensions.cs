namespace Doron.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<TResult> RunWithTimeout<TResult>(this Task<TResult> task, int timeout)
        {
            CancellationTokenSource cancellationToken = new CancellationTokenSource();

            Task timeoutTask = Task.Delay(timeout, cancellationToken.Token);

            if (await Task.WhenAny(task, timeoutTask) == timeoutTask)
                throw new TimeoutException();
            
            cancellationToken.Cancel();

            return task.Result;
        }
        
        public static Task<TResult> RunWithTimeout<TResult>(this ValueTask<TResult> task, int timeout) =>
            RunWithTimeout(task.AsTask(), timeout);
        
        public static Task RunWithTimeout(this ValueTask task, int timeout) =>
            RunWithTimeout(task.AsTask(), timeout);
        
        public static async Task RunWithTimeout(this Task task, int timeout)
        {
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            
            Task timeoutTask = Task.Delay(timeout, cancellationToken.Token);

            if (await Task.WhenAny(task, timeoutTask) == timeoutTask)
                throw new TimeoutException();
            
            cancellationToken.Cancel();
        }
    }
}
