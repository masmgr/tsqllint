using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using TSQLLint.Infrastructure.Reporters;

namespace TSQLLint
{
    public class Program
    {
        [ExcludeFromCodeCoverage]
        public static void Main(string[] args)
        {
            try
            {
                var useStdout = args.Any(arg => string.Equals(arg, "--stdout", StringComparison.Ordinal));
                if (!useStdout)
                {
                    NonBlockingConsole.WriteLine("running tsqllint");
                }

                if (args.Length == 0 && Console.IsInputRedirected)
                {
                    args = new[] { "-" };
                }

                IConsoleReporter reporter = useStdout ? new StandardErrorReporter() : new ConsoleReporter();
                var application = new Application(args, reporter);
                application.Run();

                NonBlockingConsole.ShutdownAndWait();
            }
            catch (Exception exception)
            {
                Console.WriteLine("TSQLLint encountered a problem.");
                Console.WriteLine(exception);
            }
        }
    }
}
