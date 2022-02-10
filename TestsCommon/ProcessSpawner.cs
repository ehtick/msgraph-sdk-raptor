using System.Diagnostics;

namespace TestsCommon;

public static class ProcessSpawner
{
    // create a static method that spawns a process and waits for a timeout and returns stdout and stderr as a tuple
    public static async Task<(string stdout, string stderr)> SpawnProcess(string command, string arguments, string workingDirectory, int timeout)
    {
        _ = command ?? throw new ArgumentNullException(nameof(command));
        _ = arguments ?? throw new ArgumentNullException(nameof(arguments));
        _ = workingDirectory ?? throw new ArgumentNullException(nameof(workingDirectory));

        ProcessStartInfo startInfo = new ProcessStartInfo(command, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        using Process process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        var timedOut = () => {
            process.Kill(true);
            return (string.Empty, "Process timed out while compiling!");
        };

        try
        {
            using var cts = new CancellationTokenSource(timeout);
            await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            return timedOut();
        }
        catch (OperationCanceledException)
        {
            return timedOut();
        }

        var stdout = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        return (stdout, stderr);
    }
}
