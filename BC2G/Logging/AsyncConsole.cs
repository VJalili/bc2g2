using System.Collections.Concurrent;

namespace BC2G.Logging
{
    public static class AsyncConsole
    {
        private static readonly BlockingCollection<Action> _actions = new();

        private static int _bookmarkedLine;
        private static int _addedLines = 0;

        static AsyncConsole()
        {
            var thread = new Thread(() =>
            {
                while (true) { _actions.Take()(); }
            })
            {
                IsBackground = true
            };
            thread.Start();
        }

        public static void WriteAsync(string value)
        {
            _actions.Add(() =>
            {
                Console.Write(value);
            });
        }

        public static void WriteAsync(string value, ConsoleColor color)
        {
            _actions.Add(() =>
            {
                Console.ForegroundColor = color;
                Console.Write(value);
                Console.ResetColor();
            });
        }

        public static void WriteLineAsync(string value)
        {
            _actions.Add(() =>
            {
                Console.WriteLine(value);
                _addedLines++;
            });
        }

        public static void WriteLineAsync(string value, ConsoleColor color)
        {
            _actions.Add(() =>
            {
                Console.ForegroundColor = color;
                Console.WriteLine(value);
                Console.ResetColor();
                _addedLines++;
            });
        }

        public static void BookmarkCurrentLine()
        {
            _actions.Add(() => _bookmarkedLine = Console.CursorTop);
        }

        public static void EraseToBookmarkedLine()
        {
            _actions.Add(() =>
            {
                for (int line = _bookmarkedLine + _addedLines; line >= _bookmarkedLine; line--)
                {
                    Console.CursorTop = line;
                    Console.Write(new string(' ', Console.WindowWidth - 1) + "\r");
                }

                _addedLines = 0;
            });
        }
    }
}
