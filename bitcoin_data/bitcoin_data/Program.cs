using bitcoin_data;
using bitcoin_data.Exceptions;

try
{
    var bitcoinAgent = new BitcoinAgent();
    if (bitcoinAgent.IsConnected)
    {
        var blockHeight = 714460;
        var blockHash = await bitcoinAgent.GetBlockHash(blockHeight);
        var block = await bitcoinAgent.GetBlock(blockHash);
        foreach (var tx in block.Transactions)
        {
            var g = await bitcoinAgent.GetGraph(tx);
        }
    }
    else
    {
        throw new ClientInaccessible();
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

