using System.Collections.Generic;
using NUnit.Framework;
using TSQLLint.Infrastructure.Rules;
using TSQLLint.Infrastructure.Rules.RuleViolations;

namespace TSQLLint.Tests.UnitTests.LintingRules
{
    public class NestedBlockCommentsRuleTests
    {
        private const string RuleName = "nested-block-comments";

        private static readonly object[] TestCases =
        {
            new object[]
            {
                "nested-block-comments-no-error", new List<RuleViolation>()
            },
            new object[]
            {
                "nested-block-comments-one-error", new List<RuleViolation>
                {
                    new RuleViolation(RuleName, 4, 4)
                }
            },
            new object[]
            {
                "nested-block-comments-same-line-error", new List<RuleViolation>
                {
                    new RuleViolation(RuleName, 1, 10)
                }
            }
        };

        [TestCaseSource(nameof(TestCases))]
        public void TestRule(string testFileName, List<RuleViolation> expectedRuleViolations)
        {
            RulesTestHelper.RunRulesTest(RuleName, testFileName, typeof(NestedBlockCommentsRule), expectedRuleViolations);
        }
    }
}
