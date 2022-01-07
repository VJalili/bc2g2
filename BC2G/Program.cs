using BC2G;

try
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.UserAgent.Clear();
    client.DefaultRequestHeaders.Add("User-Agent", "BitcoinAgent");

    var orchestrator = new Orchestrator(@"C:\Users\Vahid\Desktop\test\", client);
    await orchestrator.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}
