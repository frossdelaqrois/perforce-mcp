using System.Diagnostics;

namespace PerforceMcp.Perforce.Tests;

internal static class P4ProcessTestAssertions
{
    private static readonly TimeSpan ProcessExitObservationTimeout = TimeSpan.FromSeconds(2);

    public static HashSet<int> GetProcessIds()
    {
        string processName = OperatingSystem.IsWindows()
            ? "P4ValidatorTestProcess"
            : "P4ValidatorTest";
        return Process
            .GetProcessesByName(processName)
            .Select(process =>
            {
                using (process)
                {
                    return process.Id;
                }
            })
            .ToHashSet();
    }

    public static async Task AssertNoNewProcessesRemainAsync(HashSet<int> existingProcessIds)
    {
        var stopwatch = Stopwatch.StartNew();
        HashSet<int> remainingProcessIds;

        do
        {
            remainingProcessIds = GetProcessIds()
                .Except(existingProcessIds)
                .ToHashSet();
            if (remainingProcessIds.Count == 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }
        while (stopwatch.Elapsed < ProcessExitObservationTimeout);

        Assert.Empty(remainingProcessIds);
    }
}
