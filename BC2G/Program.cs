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

try
{
    var orchestrator = new Orchestrator(@"C:\Users\Vahid\Desktop\test2\", client);
    var success = await orchestrator.RunAsync(cancellationToken);
    if (!success)
        Environment.ExitCode = 1;
}
catch (Exception ex)
{
    Environment.ExitCode = 1;
    Console.Error.WriteLine(ex.Message);
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
