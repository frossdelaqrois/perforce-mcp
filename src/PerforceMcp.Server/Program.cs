using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PerforceMcp.Perforce;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
P4ExecutableDiscoveryResult executable = await new P4ExecutableDiscovery().DiscoverAsync(
    new P4ExecutableDiscoveryOptions
    {
        ExecutablePath = Environment.GetEnvironmentVariable("PERFORCE_MCP_P4_PATH"),
    });

if (!executable.IsSuccess)
{
    Console.Error.WriteLine(executable.Error?.Message ?? "Perforce executable discovery failed.");
    return 1;
}

builder.Services.AddSingleton(new P4InfoService(executable));
builder.Services.AddSingleton(new P4OpenedFilesService(executable));
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
return 0;
