namespace AutoVeritas.OffersService.Extensions;

/// <summary>
/// Bridges the migration hosted service and everything that must wait for schema:
/// the readiness health check gates on it, and any future background service awaits
/// it before first database access.
/// </summary>
public interface IMigrationCompletionSignal
{
    bool IsCompleted { get; }

    Task WaitAsync(CancellationToken cancellationToken = default);

    void SetCompleted();
}

public sealed class MigrationCompletionSignal : IMigrationCompletionSignal
{
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsCompleted => _completion.Task.IsCompleted;

    public Task WaitAsync(CancellationToken cancellationToken = default) =>
        _completion.Task.WaitAsync(cancellationToken);

    public void SetCompleted() => _completion.TrySetResult();
}
