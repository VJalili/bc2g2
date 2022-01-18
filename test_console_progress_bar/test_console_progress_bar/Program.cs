// See https://aka.ms/new-console-template for more information
using System.Collections.Concurrent;


var activeRequests = 10;
var max = 5;
while(activeRequests > max)
{

}


Queue<int> _availableRows = new();
var sss = _availableRows.TryDequeue(out int abc);

var msg = new string[] { "aaa", "bbb", "ccc", "end" };
var bar = new ProgressBar();
for(int i = 0; i< 100; i++)
{
    bar.Add(new Random().Next(10), msg[new Random().Next(4)]);
}

var run = new MovingAverage(5);
for (int i = 0; i < 100; i++)
{
    run.Add(new Random().Next());
}



var rnd = new Random();
var lines = new int[] { 0, 1, 2 , 3, 4, 5, 10, 11 };

var myList = new List<string>(new string[10]);
ConcurrentBag<int> threadIDs = new ConcurrentBag<int>();
Parallel.ForEach(myList, item => {
    threadIDs.Add(Thread.CurrentThread.ManagedThreadId);
    Thread.Sleep(1000);
});



int usedThreads = threadIDs.Distinct().Count();


for (int i=0; i< 100000; i++)
{
    Console.CursorVisible = false;
    Console.CursorTop = lines[rnd.Next(lines.Length)];
    Console.CursorLeft = 0;
    Console.Write($"--- i\t{i.ToString()}");
}

var x = 10;


class ProgressBar
{
    private readonly Queue<int> _availableRows = new();
    private readonly Dictionary<int, int> _idRowMapping = new();
    private object _locker = new object();

    public void Add(int id, string message)
    {
        lock (_locker)
        {
            if (_idRowMapping.TryGetValue(id, out int row))
            {
                if (message == "end")
                {
                    _idRowMapping.Remove(id);
                    _availableRows.Enqueue(row);
                }
            }
            else
            {
                if (!_availableRows.TryDequeue(out row))
                    row = _idRowMapping.Count;

                _idRowMapping.Add(id, row);
            }

            Console.CursorVisible = false;
            Console.CursorLeft = 0;
            Console.CursorTop = row;
            Console.Write(message);
        }
    }
}

class MovingAverage
{
    public double Average { get; private set; }

    private readonly Queue<double> _queue = new();
    private readonly int _windowSize;

    private static readonly object _locker = new();

    public MovingAverage(int windowSize)
    {
        _windowSize = windowSize;
    }

    public void Add(double runtime)
    {
        lock (_locker)
        {
            if (_queue.Count == _windowSize)
                _queue.Dequeue();
            _queue.Enqueue(runtime);
            Average = _queue.Average();
        }
    }
}
