namespace Donatello.Gateway.Extension;

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary></summary>
public static class ObservableExtensionMethods
{
    /// <summary>Subscribes an asynchronous element handler to an observable sequence.</summary>
    /// <param name="source">The observable sequence to subscribe to.</param>
    /// <param name="onNextAsync">Asynchronous function to invoke for each element in the observable sequence.</param>
    public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNextAsync)
        => source.Select(value => Observable.FromAsync(() => onNextAsync(value)))
            .Concat()
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe();

    /// <inheritdoc cref="SubscribeAsync{T}(System.IObservable{T},System.Func{T,System.Threading.Tasks.Task})"/>
    /// <param name="maximumConcurrency">Maximum number of element handlers which can be executed in parallel.</param>
    public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNextAsync, int maximumConcurrency)
        => source.Select(value => Observable.FromAsync(() => onNextAsync(value)))
            .SubscribeOn(TaskPoolScheduler.Default)
            .Merge(maximumConcurrency)
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe();

    // Copied from https://stackoverflow.com/a/58796559 and modified.
    /// <summary>Writes a debug log message when <c>Subscribe</c>, <c>OnNext</c>, <c>OnError</c>, and <c>OnCompleted</c> are called on the source sequence.</summary>
    public static IObservable<T> Spy<T>(this IObservable<T> source, ILogger logger, string observableName)
    {
        logger.LogDebug("({Name}) Observable obtained on Thread: {ThreadId}", observableName, Environment.CurrentManagedThreadId);

        var subscriptionCount = 0;
        return Observable.Create<T>(observer =>
        {
            logger.LogDebug("({Name}) Subscribed to on Thread: {ThreadId}", observableName, Environment.CurrentManagedThreadId);
            try
            {
                var subscription = source
                    .Do(x => logger.LogDebug("({Name}) OnNext -> {ElementType} on Thread: {ThreadId}", observableName, x.GetType(), Environment.CurrentManagedThreadId),
                        ex => logger.LogDebug("({Name}) OnError -> {Exception} on Thread: {ThreadId}", observableName, ex.GetType(), Environment.CurrentManagedThreadId),
                        () => logger.LogDebug("({Name}) OnCompleted on Thread: {ThreadId}", observableName, Environment.CurrentManagedThreadId)
                    )
                    .Subscribe(t =>
                    {
                        try
                        {
                            observer.OnNext(t);
                        }
                        catch (Exception ex)
                        {
                            logger.LogDebug("({Name}) Downstream exception ({Exception}) on Thread: {ThreadId}", observableName, ex, Environment.CurrentManagedThreadId);
                            throw;
                        }
                    }, observer.OnError, observer.OnCompleted);

                return new CompositeDisposable(
                    Disposable.Create(() => logger.LogDebug("({Name}) Dispose (Unsubscribe or Observable finished) on Thread: {ThreadId}", observableName, Environment.CurrentManagedThreadId)),
                    subscription,
                    Disposable.Create(() => Interlocked.Decrement(ref subscriptionCount)),
                    Disposable.Create(() => logger.LogDebug("({Name}) Dispose (Unsubscribe or Observable finished) completed, {Count} subscriptions", observableName, subscriptionCount))
                );
            }
            finally
            {
                Interlocked.Increment(ref subscriptionCount);
                logger.LogDebug("({Name}) Subscription completed, {Count} subscriptions.", observableName, subscriptionCount);
            }
        });
    }
}