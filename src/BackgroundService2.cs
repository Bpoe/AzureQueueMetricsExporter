namespace AzQueueMetricExporter;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

/// <summary>
/// Base class for implementing a long running <see cref="IHostedService"/>.
/// </summary>
public abstract class BackgroundService2 : IHostedService, IDisposable
{
    private Task? _executeTask;
    private CancellationTokenSource? _stoppingCts;

    /// <summary>
    /// Gets the Task that executes the background operation.
    /// </summary>
    /// <remarks>
    /// Will return <see langword="null"/> if the background operation hasn't started.
    /// </remarks>
    public virtual Task? ExecuteTask => _executeTask;

    /// <summary>
    /// This method is called when the <see cref="IHostedService"/> starts. The implementation should return a task that represents
    /// the lifetime of the long running operation(s) being performed.
    /// </summary>
    /// <param name="stoppingToken">Triggered when <see cref="IHostedService.StopAsync(CancellationToken)"/> is called.</param>
    /// <returns>A <see cref="Task"/> that represents the long running operations.</returns>
    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        // Create linked token to allow cancelling executing task from provided token
        this._stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Store the task we're executing
        this._executeTask = this.ExecuteAsync(this._stoppingCts.Token);

        // If the task is completed then return it, this will bubble cancellation and failure to the caller
        if (this._executeTask.IsCompleted)
        {
            return this._executeTask;
        }

        // Otherwise it's running
        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop called without start
        if (this._executeTask == null)
        {
            return;
        }

        try
        {
            // Signal cancellation to the executing method
            this._stoppingCts?.Cancel();
        }
        finally
        {
            // Wait until the task completes or the stop token triggers
            var final = await Task.WhenAny(this._executeTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);

            // This is the reason we had to re-implement this class!
            // WhenAny returns a Task<Task>. Its own Task is RanToCompletion without any exceptions, so any exceptions
            // from _executeTask won't be bubbled up.
            // So we can await its result Task that completed and if it had any exceptions, those will now be observed.
            await final;
        }
    }

    public virtual void Dispose() => this._stoppingCts?.Cancel();
}
