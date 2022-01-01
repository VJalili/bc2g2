﻿namespace BC2G.Graph
{
    public class BlockGraph : GraphBase
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
