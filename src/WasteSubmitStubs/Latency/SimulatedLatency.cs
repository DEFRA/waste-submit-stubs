namespace WasteSubmitStubs.Latency;

/// <summary>
/// A simulated upstream delay: a fixed part, plus an even random 0 to <c>jitterMs</c> on top, so responses don't
/// all take exactly the same time. Both default to 0, so nothing is added unless it's configured.
/// </summary>
public static class SimulatedLatency
{
    public static TimeSpan Next(int latencyMs, int jitterMs, Random random) =>
        TimeSpan.FromMilliseconds(latencyMs + (jitterMs > 0 ? random.Next(jitterMs + 1) : 0));

    /// <summary>Waits for the next delay. Stops early, without an error, if the caller has gone.</summary>
    public static async Task Wait(int latencyMs, int jitterMs, CancellationToken cancellationToken)
    {
        var delay = Next(latencyMs, jitterMs, Random.Shared);
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            // The client gave up; the response is never read.
        }
    }
}
