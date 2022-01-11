﻿using BC2G;

var tokenSource = new CancellationTokenSource();
var cancellationToken = tokenSource.Token;

AppDomain.CurrentDomain.ProcessExit +=
(sender, e) => ProcessExit(sender, e, tokenSource);

Console.CancelKeyPress += new ConsoleCancelEventHandler(
    (sender, e) => CancelKeyPressHandler(sender, e, tokenSource));


var client = new HttpClient();
client.DefaultRequestHeaders.Accept.Clear();
client.DefaultRequestHeaders.UserAgent.Clear();
client.DefaultRequestHeaders.Add("User-Agent", "BitcoinAgent");

var orchestrator = new Orchestrator(@"C:\Users\Vahid\Desktop\test\", client);

try
{
    await orchestrator.RunAsync(cancellationToken);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}
finally
{
    tokenSource.Dispose();
}

static void ProcessExit(
    object? sender, EventArgs e,
    CancellationTokenSource tokenSource)
{
    if (!tokenSource.IsCancellationRequested)
        tokenSource.Cancel();
    
    Console.WriteLine("Exiting application.");
}

static void CancelKeyPressHandler(
    object? sender, ConsoleCancelEventArgs e,
    CancellationTokenSource tokenSource)
{
    tokenSource.Cancel();
    e.Cancel = true;
    Console.WriteLine("Cancelling ... do not turn off your computer.");
}
