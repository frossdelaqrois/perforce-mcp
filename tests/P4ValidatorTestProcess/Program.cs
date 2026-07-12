if (args.Length == 0 || args[0] == "-V" || args[0] == "wait")
{
    await Task.Delay(Timeout.InfiniteTimeSpan);
    return;
}

switch (args[0])
{
    case "success":
        Console.Write("success");
        Console.Error.Write("diagnostic");
        break;
    case "arguments":
        Console.Write(string.Join("\n", args.Skip(1)));
        break;
    case "failure":
        Console.Error.Write("password=hunter2 ticket:ABC123 known-secret");
        Environment.ExitCode = 7;
        break;
    case "oversized-output":
        string output = new('x', 40 * 1024);
        await Console.Out.WriteAsync(output);
        await Console.Error.WriteAsync(output);
        break;
    default:
        Console.Error.Write("unknown mode");
        Environment.ExitCode = 2;
        break;
}
