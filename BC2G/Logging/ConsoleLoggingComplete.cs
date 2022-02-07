namespace BC2G.Logging
{
    public class ConsoleLoggingComplete : ConsoleLoggingBase
    {
        private static readonly ConsoleColor[] _colors = new[]
        {
            ConsoleColor.Cyan,
            ConsoleColor.Blue,
            ConsoleColor.Blue,
            ConsoleColor.Blue,
            ConsoleColor.Blue,
            ConsoleColor.Blue,
        };

        public ConsoleLoggingComplete(int fromInclusive, int toExclusive) :
            base(fromInclusive, toExclusive, 6)
        { }

        public override void Log(int height)
        {
            string[] msgs = new[]
            {
                $"\tCurrent:\t{height,10:n0}",
                $"\tCompleted:\t{Completed,10:n0}/{Total:n0} ({Percentage:f2}%)",
                $"\tBlock Rage:\t{BlockRuntimeMovingAvg.Speed,10:n0} bps",
                $"\tEdge Rage:\t{EdgeRuntimeMovingAvg.Speed,10:n0} eps",
                $"\tNodes:\t{NodesCount,10:n0}",
                $"\tEdges:\t{EdgesCount,10:n0}"
            };

            AsyncConsole.WriteLines(msgs, _colors);
        }

        public override void Log(
            int height, 
            int allNodesCount, 
            int addedEdgesCount, 
            double runtime)
        {
            base.Log(height, allNodesCount, addedEdgesCount, runtime);
            Log(height);
        }
    }
}
