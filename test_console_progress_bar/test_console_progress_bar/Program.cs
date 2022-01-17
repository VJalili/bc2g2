// See https://aka.ms/new-console-template for more information
using System.Collections.Concurrent;


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
