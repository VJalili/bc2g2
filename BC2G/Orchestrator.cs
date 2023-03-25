﻿using BC2G.CLI;

using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace BC2G;

public class Orchestrator : IDisposable
{
    private readonly Cli _cli;
    private ILogger? _logger;
    private readonly CancellationToken _cT;

    private bool _disposed = false;

    public Orchestrator(CancellationToken cancelationToken)
    {
        _cT = cancelationToken;

        _cli = new Cli(
            TraverseBitcoinAsync,
            SampleGraphAsync,
            ImportGraphAsync,
            (e, c) =>
            {
                if (_logger != null)
                    _logger.LogCritical("{error}", e.Message);
                else
                    Console.Error.WriteLine($"Error: {e.Message}");
            });
    }

    public async Task<int> InvokeAsync(string[] args)
    {
        return await _cli.InvokeAsync(args);
    }

    private async Task<IHost> SetupAndGetHostAsync(Options options)
    {
        Directory.CreateDirectory(options.WorkingDir);
        var hostBuilder = Startup.GetHostBuilder(options);
        var host = hostBuilder.Build();
        await host.StartAsync();
        _logger = host.Services.GetRequiredService<ILogger<Orchestrator>>();
        return host;
    }

    private async Task TraverseBitcoinAsync(Options options)
    {
        var host = await SetupAndGetHostAsync(options);
        using (var dbContext = host.Services.GetRequiredService<DatabaseContext>())
            await dbContext.Database.EnsureCreatedAsync(_cT);

        var bitcoinOrchestrator = host.Services.GetRequiredService<BitcoinOrchestrator>();
        await bitcoinOrchestrator.TraverseAsync(options, _cT);
    }

    private async Task ImportGraphAsync(Options options)
    {
        var host = await SetupAndGetHostAsync(options);
        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, _cT);

        var graphDb = host.Services.GetRequiredService<IGraphDb<BitcoinGraph>>();
        await graphDb.ImportAsync(_cT);
    }

    private async Task SampleGraphAsync(Options options)
    {
        var host = await SetupAndGetHostAsync(options);
        await JsonSerializer<Options>.SerializeAsync(options, options.StatusFile, _cT);
        var graphDb = host.Services.GetRequiredService<IGraphDb<BitcoinGraph>>();
        var successfull = await graphDb.TrySampleAsync();
        if (successfull)
            _logger?.LogInformation("Successfully completed sampling graphs.");
        else
            _logger?.LogError("Faild sampling graphs with the given parameters.");
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            { }

            _disposed = true;
        }
    }
}
