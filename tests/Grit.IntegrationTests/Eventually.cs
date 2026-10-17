namespace Grit.IntegrationTests;

// The saga and the stock consumer run after the HTTP call answers; tests poll for the end state, never wait for absence.
// A timeout fails here, with the last value read: returning that value let the caller's next assertion blame the wrong field.
internal static class Eventually
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);

    public static async Task<T> UntilAsync<T>(Func<Task<T>> read, Func<T, bool> isDone)
    {
        var giveUpAt = DateTime.UtcNow + Deadline;
        while (true)
        {
            var value = await read();
            if (isDone(value))
            {
                return value;
            }

            if (DateTime.UtcNow > giveUpAt)
            {
                throw new TimeoutException($"Not done after {Deadline.TotalSeconds} seconds. Last value read: {value}");
            }

            await Task.Delay(200, TestContext.Current.CancellationToken);
        }
    }
}
