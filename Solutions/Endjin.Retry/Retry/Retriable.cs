﻿namespace Endjin.Core.Retry
{
    #region Using statements

    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Endjin.Core.Async.Contracts;
    using Endjin.Core.Retry.Policies;
    using Endjin.Core.Retry.Strategies;

    #endregion

    public static class Retriable
    {
        public static ISleepService SleepService { get; set; }

        public static T Retry<T>(Func<T> func)
        {
            return Retry(func, CancellationToken.None, new Count(10), new AnyException());            
        }

        private static T Retry<T>(Func<T> func, CancellationToken cancellationToken, IRetryStrategy strategy, IRetryPolicy policy)
        {
            do
            {
                try
                {
                    return func();
                }
                catch (Exception exception)
                {
                    var delay = strategy.PrepareToRetry(exception);

                    if (!WillRetry(exception, cancellationToken, strategy, policy))
                    {
                        throw;
                    }

                    strategy.OnRetrying(new RetryEventArgs(exception, delay));

                    if (delay != TimeSpan.Zero)
                    {
                        if (SleepService != null)
                        {
                            SleepService.Sleep(delay);
                        }
                    }
                }
            }
            while (true);
        }


        public static Task<T> RetryAsync<T>(
            Func<Task<T>> asyncFunc)
        {
            return RetryAsync(asyncFunc, CancellationToken.None, new Count(10), new AnyException());
        }

        private static async Task<T> RetryAsync<T>(Func<Task<T>> asyncFunc, CancellationToken cancellationToken, IRetryStrategy strategy, IRetryPolicy policy)
        {
            do
            {
                Exception exception;

                try
                {
                    return await asyncFunc();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                var delay = strategy.PrepareToRetry(exception);

                if (!WillRetry(exception, cancellationToken, strategy, policy))
                {
                    throw exception;
                }

                strategy.OnRetrying(new RetryEventArgs(exception, delay));

                if (delay != TimeSpan.Zero)
                {
                    if (SleepService != null)
                    {
                        SleepService.Sleep(delay);
                    }
                }
            }
            while (true);
        }

        private static bool WillRetry(Exception exception, CancellationToken cancellationToken, IRetryStrategy strategy, IRetryPolicy policy)
        {
            return strategy.CanRetry && !cancellationToken.IsCancellationRequested && policy.CanRetry(exception);
        }
    }
}