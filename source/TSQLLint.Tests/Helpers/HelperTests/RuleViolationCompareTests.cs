using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using TSQLLint.Infrastructure.Rules.RuleViolations;
using TSQLLint.Tests.Helpers.ObjectComparers;

namespace TSQLLint.Tests.Helpers.HelperTests
{
    public class RuleViolationCompareTests
    {
        public static readonly object[] NonEquivalentRuleViolations =
        {
            new object[]
            {
                new List<RuleViolation>
                {
                    new RuleViolation("some-rule", 99, 0),
                    new RuleViolation("some-rule", 0, 0)
                }
            },
            new object[]
            {
                new List<RuleViolation>
                {
                    new RuleViolation("some-rule", 0, 0),
                    new RuleViolation("some-rule", 0, 99)
                }
            },
            new object[]
            {
                new List<RuleViolation>
                {
                    new RuleViolation("some-rule", 0, 0),
                    new RuleViolation("foo", 0, 0)
                }
            }
        };

        public static readonly object[] EquivalentRuleViolations =
        {
            new object[]
            {
                new List<RuleViolation>
                {
                    new RuleViolation("some-rule", 0, 1),
                    new RuleViolation("some-rule", 0, 1)
                }
            }
        };

        private readonly RuleViolationComparer ruleViolationComparer = new RuleViolationComparer();

        [Test]
        [TestCaseSource(nameof(EquivalentRuleViolations))]
        public void CompareEquivalentRulesTest(List<RuleViolation> ruleViolations)
        {
            Assert.AreEqual(0, ruleViolationComparer.Compare(ruleViolations[0], ruleViolations[1]));
        }

        [Test]
        [TestCaseSource(nameof(NonEquivalentRuleViolations))]
        public void CompareNonEquivalentRulesTest(List<RuleViolation> ruleViolations)
        {
            Assert.AreEqual(-1, ruleViolationComparer.Compare(ruleViolations[0], ruleViolations[1]));
        }

        [Test]
        [ExcludeFromCodeCoverage]
        public void CompareRulesShouldThrow()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                ruleViolationComparer.Compare(new object(), new object());
            });

            Assert.That(ex.Message, Is.EqualTo("cannot compare null object"));
        }
    }
}
