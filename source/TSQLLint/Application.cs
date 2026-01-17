using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using Microsoft.Extensions.FileSystemGlobbing;
using TSQLLint.Common;
using TSQLLint.Core.DTO;
using TSQLLint.Core.Interfaces;
using TSQLLint.Core.Interfaces.Config.Contracts;
using TSQLLint.Core.UseCases.Console;
using TSQLLint.Infrastructure.CommandLineOptions;
using TSQLLint.Infrastructure.Configuration;
using TSQLLint.Infrastructure.Parser;
using TSQLLint.Infrastructure.Plugins;
using TSQLLint.Infrastructure.Reporters;
using TSQLLint.Infrastructure.Rules.RuleExceptions;

namespace TSQLLint
{
    public class Application
    {
        private readonly IRequestHandler<CommandLineRequestMessage, HandlerResponseMessage> commandLineOptionHandler;
        private readonly ICommandLineOptions commandLineOptions;
        private readonly IConfigReader configReader;
        private readonly IConsoleReporter reporter;
        private readonly IConsoleTimer timer;
        private readonly IIgnoreListReader ignoreListReader;

        private IPluginHandler pluginHandler;
        private ISqlFileProcessor fileProcessor;

        public Application(string[] args, IConsoleReporter reporter)
        {
            timer = new ConsoleTimer();
            timer.Start();

            this.reporter = reporter;
            commandLineOptions = new CommandLineOptions(args);
            configReader = new ConfigReader(reporter);
            ignoreListReader = new IgnoreListReader(reporter);
            commandLineOptionHandler = new CommandLineOptionHandler(
                new ConfigFileGenerator(),
                configReader,
                reporter,
                new FileSystemWrapper());
        }

        public void Run()
        {
            configReader.LoadConfig(commandLineOptions.ConfigFile);
            ignoreListReader.LoadIgnoreList(commandLineOptions.IgnoreListFile);

            var response = commandLineOptionHandler.Handle(new CommandLineRequestMessage(commandLineOptions));

            if (response.ShouldLint)
            {
                var shouldFix = response.ShouldFix;
                var stdinRequested = commandLineOptions.LintPath != null
                    && commandLineOptions.LintPath.Any(path => string.Equals(path, "-", StringComparison.Ordinal));
                var stdoutRequested = commandLineOptions.Stdout;
                if (stdoutRequested && !stdinRequested)
                {
                    reporter.Report("The --stdout option is only supported when linting from stdin.");
                    Environment.ExitCode = 1;
                    return;
                }

                if (shouldFix && stdinRequested && !stdoutRequested)
                {
                    reporter.Report("Cannot use --fix when linting from stdin without --stdout.");
                    shouldFix = false;
                }

                if (shouldFix && stdinRequested && stdoutRequested)
                {
                    FixStandardInputToStdout();
                    return;
                }

                int? firstViolitionCount = null;
                List<IRuleViolation> violitions = null;
                List<IRuleViolation> previousViolations = null;
                const int maxPasses = 10;
                var matcher = new Matcher();
                matcher.AddInclude("**/*.sql").AddExcludePatterns(ignoreListReader.IgnoreList);
                var globPatternMatcher = new GlobPatternMatcher(matcher);
                var passCount = 0;

                do
                {
                    var fragmentBuilder = new FragmentBuilder(configReader.CompatibilityLevel);
                    var rules = RuleVisitorFriendlyNameTypeMap.Rules;
                    var ruleVisitorBuilder = new RuleVisitorBuilder(configReader, this.reporter, rules);
                    var ruleVisitor = new SqlRuleVisitor(ruleVisitorBuilder, fragmentBuilder, reporter);
                    pluginHandler = new PluginHandler(reporter, rules);
                    pluginHandler.ProcessPaths(configReader.GetPlugins());
                    fileProcessor = new SqlFileProcessor(
                        ruleVisitor, pluginHandler, reporter, new FileSystem(), rules.ToDictionary(x => x.Key, x => x.Value.GetType()), globPatternMatcher);

                    passCount++;
                    previousViolations = violitions;

                    reporter.ShouldCollectViolations = shouldFix;
                    reporter.ClearViolations();
                    fileProcessor.ProcessList(commandLineOptions.LintPath);

                    // Prevent the reporter from double or triple counting errors if the while loop evaluates to true;
                    reporter.ReporterMuted = true;

                    if (shouldFix)
                    {
                        new ViolationFixer(new FileSystem(), rules, reporter.Violations).Fix();

                        violitions = reporter.Violations;

                        if (!firstViolitionCount.HasValue)
                        {
                            firstViolitionCount = violitions.Count;
                        }
                    }
                }
                while (shouldFix && violitions.Count > 0 && !AreEqual(violitions, previousViolations) && passCount < maxPasses);

                if (fileProcessor.FileCount > 0)
                {
                    reporter.FixedCount = firstViolitionCount - violitions?.Count;
                    reporter.ReportResults(timer.Stop(), fileProcessor.FileCount);
                }
            }

            if (!response.Success)
            {
                Environment.ExitCode = 1;
            }
        }

        private static string ApplyFixesToText(string sqlText, IDictionary<string, ISqlLintRule> rules, IList<IRuleViolation> violations)
        {
            var fileViolations = violations
                .OrderByDescending(x => x.Line)
                .ThenByDescending(x => x.Column)
                .ToList();

            var fileLines = ReadLines(sqlText);
            var fileLineActions = new FileLineActions(fileViolations, fileLines);

            foreach (var violation in fileViolations)
            {
                if (!rules.ContainsKey(violation.RuleName))
                {
                    continue;
                }

                if (violation.Line <= 0 || violation.Line > fileLines.Count)
                {
                    continue;
                }

                if (violation.Line == 1 && violation.Column > fileLines[violation.Line - 1].Length + 1)
                {
                    continue;
                }

                var lines = new List<string>(fileLines);
                rules[violation.RuleName].FixViolation(lines, violation, fileLineActions);
            }

            var builder = new StringBuilder();
            foreach (var line in fileLines)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        private static List<string> ReadLines(string sqlText)
        {
            var lines = new List<string>();
            using (var reader = new StringReader(sqlText))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }

        private static bool IsWholeInputIgnored(IEnumerable<IExtendedRuleException> ignoredRules, string sqlText)
        {
            var ignoredRulesEnum = ignoredRules.ToArray();
            if (!ignoredRulesEnum.Any())
            {
                return false;
            }

            var lineOneRuleIgnores = ignoredRulesEnum.OfType<GlobalRuleException>().Where(x => x.StartLine == 1).ToArray();
            if (!lineOneRuleIgnores.Any())
            {
                return false;
            }

            var lineCount = ReadLines(sqlText).Count;
            return lineOneRuleIgnores.Any(x => x.EndLine == lineCount);
        }

        private bool AreEqual(List<IRuleViolation> violitions, List<IRuleViolation> previousViolations)
        {
            return violitions.All(x => previousViolations?.Any(y
                => x.RuleName == y.RuleName && x.Line == y.Line && x.Column == y.Column) == true) &&
                previousViolations?.All(x => violitions.Any(y
                    => x.RuleName == y.RuleName && x.Line == y.Line && x.Column == y.Column)) == true;
        }

        private void FixStandardInputToStdout()
        {
            var sqlText = Console.In.ReadToEnd();
            var sqlPath = "-";
            int? firstViolitionCount = null;
            List<IRuleViolation> violitions = null;
            List<IRuleViolation> previousViolations = null;
            const int maxPasses = 10;
            var passCount = 0;

            do
            {
                var fragmentBuilder = new FragmentBuilder(configReader.CompatibilityLevel);
                var rules = RuleVisitorFriendlyNameTypeMap.Rules;
                var ruleVisitorBuilder = new RuleVisitorBuilder(configReader, this.reporter, rules);
                var ruleVisitor = new SqlRuleVisitor(ruleVisitorBuilder, fragmentBuilder, reporter);
                pluginHandler = new PluginHandler(reporter, rules);
                pluginHandler.ProcessPaths(configReader.GetPlugins());
                var ruleTypeMap = rules.ToDictionary(x => x.Key, x => x.Value.GetType());
                var ruleExceptionFinder = new RuleExceptionFinder(ruleTypeMap);

                passCount++;
                previousViolations = violitions;

                reporter.ShouldCollectViolations = true;
                reporter.ClearViolations();

                using (var sqlStream = new MemoryStream(Console.InputEncoding.GetBytes(sqlText)))
                {
                    var ignoredRules = ruleExceptionFinder.GetIgnoredRuleList(sqlStream).ToList();

                    if (!IsWholeInputIgnored(ignoredRules, sqlText))
                    {
                        ruleVisitor.VisitRules(sqlPath, ignoredRules, sqlStream);
                        sqlStream.Position = 0;
                        TextReader textReader = new StreamReader(sqlStream);
                        pluginHandler.ActivatePlugins(new PluginContext(sqlPath, ignoredRules, textReader));
                    }
                }

                // Prevent the reportor from douple or tripple counting errors if the while loop evaulates to true;
                reporter.ReporterMuted = true;

                violitions = reporter.Violations;

                if (!firstViolitionCount.HasValue)
                {
                    firstViolitionCount = violitions.Count;
                }

                if (violitions.Count > 0)
                {
                    sqlText = ApplyFixesToText(sqlText, rules, violitions);
                }
            }
            while (violitions.Count > 0 && !AreEqual(violitions, previousViolations) && passCount < maxPasses);

            reporter.FixedCount = firstViolitionCount - violitions?.Count;
            reporter.ReportResults(timer.Stop(), 1);
            Console.Out.Write(sqlText);
        }
    }
}
