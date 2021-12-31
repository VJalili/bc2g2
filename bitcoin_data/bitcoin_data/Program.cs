using bitcoin_data;

try
{
    var client = new HttpClient();
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.UserAgent.Clear();
    client.DefaultRequestHeaders.Add("User-Agent", "BitcoinAgent");

    var orchestrator = new Orchestrator(@"C:\Users\Vahid\Desktop\test\", client);
    await orchestrator.RunAsync();

    // Try these transactions, debug/test
    // 700000; //199233; //714460; //100; 3000; 30000; ****300000***
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}
