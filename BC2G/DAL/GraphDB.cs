using Neo4j.Driver;

namespace BC2G.DAL
{
    public class GraphDB : IDisposable
    {
        private bool _disposed = false;
        private readonly IDriver _driver;

        ~GraphDB() => Dispose(false);

        public GraphDB(string uri, string user, string password)
        {
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
        }

        public async void PrintGreeting(string message)
        {
            using (var session = _driver.AsyncSession())
            {
                var greeting = session.WriteTransactionAsync(async tx =>
                {
                    var result = await tx.RunAsync("CREATE (a:Greeting) " +
                                        "SET a.message = $message " +
                                        "RETURN a.message + ', from node ' + id(a)",
                        new { message });
                    return result.SingleAsync().Result[0].As<string>();
                });
                Console.WriteLine(greeting);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _driver?.Dispose();
            }

            _disposed = true;
        }

        public static void Run()
        {
            using (var greeter = new GraphDB("bolt://localhost:7687", "neo4j", "password"))
            {
                greeter.PrintGreeting("hello, world");
            }
        }
    }
}
