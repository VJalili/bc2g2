using bitcoin_data;
using bitcoin_data.Exceptions;
using bitcoin_data.Model;
using bitcoin_data.Serializers;


var orchestrator = new Orchestrator(@"C:\Users\Vahid\Desktop\test\");
await orchestrator.Run();

string outputDir = @"C:\Users\Vahid\Desktop\test\";

try
{
    var tmpFile = Path.Combine(outputDir, "tmp_access_test");
    File.Create(tmpFile).Dispose();
    File.Delete(tmpFile);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Require write access to the path {outputDir}: {ex.Message}");
    Environment.Exit(1);
}


string addressIdFilename = Path.Combine(outputDir, "address_id.csv");
string statusFile = Path.Combine(outputDir, "status.json");



Status status = new();
try
{
    status = await JsonSerializer<Status>.DeserializeAsync(statusFile);
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}

try
{
    var bitcoinAgent = new BitcoinAgent();
    if (bitcoinAgent.IsConnected)
    {
        var nextBlockHeight = status.LastBlockHeight + 1;
        var blockHeight = nextBlockHeight; 
        // Try these transactions, debug/test
        // 700000; //199233; //714460; //100; 3000; 30000; ****300000***
        var blockHash = await bitcoinAgent.GetBlockHash(blockHeight);
        var block = await bitcoinAgent.GetBlock(blockHash);
        var graph = await bitcoinAgent.GetGraph(block);

        var serializer = new CSVSerializer(addressIdFilename);
        serializer.Serialize(graph,
            Path.Combine(outputDir, $"{nextBlockHeight}"));
        //status.LastBlockHeight += 1;
        //await JsonSerializer<Status>.SerializeAsync(status, statusFile);
    }
    else
    {
        throw new ClientInaccessible();
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
}


