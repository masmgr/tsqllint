using System;
using System.Collections.Concurrent;
using System.Threading;

namespace TSQLLint.Infrastructure.Reporters
{
    public static class NonBlockingConsole
    {
        public static BlockingCollection<string> messageQueue = new BlockingCollection<string>();
        private static Thread consumerThread;

        static NonBlockingConsole()
        {
            consumerThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                foreach (var message in messageQueue.GetConsumingEnumerable())
                {
                    Console.WriteLine(message);
                }
            });
            consumerThread.Start();
        }

        public static void WriteLine(string value)
        {
            messageQueue.Add(value);
        }

        public static void ShutdownAndWait()
        {
            messageQueue.CompleteAdding();
            consumerThread?.Join(TimeSpan.FromSeconds(5));
            messageQueue.Dispose();
        }
    }
}
