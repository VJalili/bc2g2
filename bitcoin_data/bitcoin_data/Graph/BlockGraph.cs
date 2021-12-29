using bitcoin_data.Model;

namespace bitcoin_data.Graph
{
    internal class BlockGraph : BaseGraph
    {
        public void AddGraph(CoinbaseTransactionGraph coinbaseTxGraph)
        {
            coinbaseTxGraph.UpdateEdges();
            AddEdges(coinbaseTxGraph.Edges);
        }

        public void AddGraph(TransactionGraph txGraph)
        {
            txGraph.UpdateEdges();
            AddEdges(txGraph.Edges);
        }
    }
}
