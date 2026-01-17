using System.Collections.Generic;
using NUnit.Framework;
using TSQLLint.Infrastructure.Rules;
using TSQLLint.Infrastructure.Rules.RuleViolations;

namespace TSQLLint.Tests.UnitTests.LintingRules
{
    public class UtcDateTimeRuleTests
    {
        private const string RuleName = "utc-datetime";

        private static readonly object[] TestCases =
        {
            new object[]
            {
                "utc-datetime-no-error", new List<RuleViolation>()
            },
            new object[]
            {
                "utc-datetime-one-error", new List<RuleViolation>
                {
                    new RuleViolation(RuleName, 1, 8)
                }
            },
            new object[]
            {
                "utc-datetime-two-errors", new List<RuleViolation>
                {
                    new RuleViolation(RuleName, 1, 8),
                    new RuleViolation(RuleName, 2, 8)
                }
            },
            new object[]
            {
                "utc-datetime-one-error-mixed-state", new List<RuleViolation>
                {
                    new RuleViolation(RuleName, 2, 8)
                }
            }
        };

        [TestCaseSource(nameof(TestCases))]
        public void TestRule(string testFileName, List<RuleViolation> expectedRuleViolations)
        {
            RulesTestHelper.RunRulesTest(RuleName, testFileName, typeof(UtcDateTimeRule), expectedRuleViolations);
        }
    }
}
