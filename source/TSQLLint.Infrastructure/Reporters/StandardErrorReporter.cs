using System;

namespace TSQLLint.Infrastructure.Reporters
{
    public class StandardErrorReporter : ConsoleReporter
    {
        public override void Report(string message)
        {
            Console.Error.WriteLine(message);
        }
    }
}
