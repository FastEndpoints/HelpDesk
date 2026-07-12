using System.Diagnostics;
using System.Runtime.InteropServices;

if (args.Length == 0)
{
    Console.Error.WriteLine("At least one service assembly path is required.");
    return 1;
}

using var stopping = new CancellationTokenSource();
using var sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandleSignal);
using var sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, HandleSignal);

var children = new List<Process>();

try
{
    foreach (var assemblyPath in args)
    {
        var child = Process.Start(
            new ProcessStartInfo
            {
                FileName = Environment.ProcessPath ?? "dotnet",
                UseShellExecute = false,
                ArgumentList = { assemblyPath }
            });

        if (child is null)
            throw new InvalidOperationException($"Failed to start {assemblyPath}.");

        children.Add(child);
        Console.WriteLine($"Started {Path.GetFileNameWithoutExtension(assemblyPath)} (PID {child.Id}).");
    }

    var exitTasks = children.Select(child => child.WaitForExitAsync()).ToArray();
    var anyChildExited = Task.WhenAny(exitTasks);
    var stoppingRequested = Task.Delay(Timeout.InfiniteTimeSpan, stopping.Token);
    var completed = await Task.WhenAny(anyChildExited, stoppingRequested);

    if (completed == anyChildExited)
    {
        var exitTask = await anyChildExited;
        var index = Array.IndexOf(exitTasks, exitTask);
        var exitedChild = children[index];
        await exitTask;
        Console.Error.WriteLine($"{Path.GetFileName(args[index])} exited unexpectedly with code {exitedChild.ExitCode}; stopping sibling services.");
        await StopChildrenAsync(children);
        return exitedChild.ExitCode == 0 ? 1 : exitedChild.ExitCode;
    }

    await StopChildrenAsync(children);
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    await StopChildrenAsync(children);
    return 1;
}
finally
{
    foreach (var child in children)
        child.Dispose();
}

void HandleSignal(PosixSignalContext context)
{
    context.Cancel = true;
    stopping.Cancel();
}

static async Task StopChildrenAsync(IReadOnlyCollection<Process> children)
{
    foreach (var child in children.Where(child => !child.HasExited))
        _ = kill(child.Id, PosixSignal.SIGTERM);

    try
    {
        await Task.WhenAll(children.Select(child => child.WaitForExitAsync())).WaitAsync(TimeSpan.FromSeconds(30));
    }
    catch (TimeoutException)
    {
        foreach (var child in children.Where(child => !child.HasExited))
            child.Kill(entireProcessTree: true);

        await Task.WhenAll(children.Select(child => child.WaitForExitAsync()));
    }
}

[DllImport("libc", SetLastError = true)]
static extern int kill(int pid, PosixSignal signal);
