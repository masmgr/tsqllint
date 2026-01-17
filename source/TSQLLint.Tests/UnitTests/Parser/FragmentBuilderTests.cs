using System.IO;
using NUnit.Framework;
using TSQLLint.Infrastructure.Parser;

namespace TSQLLint.Tests.UnitTests.Parser
{
    [TestFixture]
    public class FragmentBuilderTests
    {
        private const string CreateOrAlterProcedureSql = @"CREATE OR ALTER PROCEDURE dbo.TestProc
AS
BEGIN
    SELECT 1;
END;";

        private const string CreateOrAlterTriggerSql = @"CREATE OR ALTER TRIGGER dbo.TestTrigger
ON dbo.TestTable
AFTER INSERT
AS
BEGIN
    SELECT 1;
END;";

        private const string CreateOrAlterFunctionSql = @"CREATE OR ALTER FUNCTION dbo.TestFunc()
RETURNS INT
AS
BEGIN
    RETURN 1;
END;";

        private const string CreateOrAlterViewSql = @"CREATE OR ALTER VIEW dbo.TestView
AS
SELECT 1 AS Col1;";

        private const string CreateOrAlterWithSetStatementsSql = @"SET QUOTED_IDENTIFIER ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
SET NOCOUNT ON;

CREATE OR ALTER PROCEDURE dbo.TestProc
AS
BEGIN
    DECLARE @Var1 VARCHAR(100);
    SELECT @Var1 = 'Test';
    SELECT @Var1;
END;";

        [TestCase("CREATE OR ALTER PROCEDURE", CreateOrAlterProcedureSql)]
        [TestCase("CREATE OR ALTER TRIGGER", CreateOrAlterTriggerSql)]
        [TestCase("CREATE OR ALTER FUNCTION", CreateOrAlterFunctionSql)]
        [TestCase("CREATE OR ALTER VIEW", CreateOrAlterViewSql)]
        public void GetFragment_CreateOrAlterSyntax_ShouldParseWithoutErrors(string description, string sql)
        {
            // arrange
            var fragmentBuilder = new FragmentBuilder(150); // compatibility level 150 supports CREATE OR ALTER
            var stream = ParsingUtility.GenerateStreamFromString(sql);
            var textReader = new StreamReader(stream);

            // act
            var fragment = fragmentBuilder.GetFragment(textReader, out var errors);

            // assert
            Assert.IsNotNull(fragment, $"{description}: Fragment should not be null");
            Assert.IsEmpty(errors, $"{description}: No parsing errors should occur. Errors: {string.Join(", ", errors)}");
            Assert.AreNotEqual(-1, fragment.FirstTokenIndex, $"{description}: Fragment should have valid token index");
        }

        [TestCase("CREATE OR ALTER TRIGGER with compatibility level 130", CreateOrAlterTriggerSql, 130)]
        public void GetFragment_CreateOrAlterSyntax_WithCompatibilityLevel_ShouldParseCorrectly(
            string description,
            string sql,
            int compatibilityLevel)
        {
            // arrange
            var fragmentBuilder = new FragmentBuilder(compatibilityLevel);
            var stream = ParsingUtility.GenerateStreamFromString(sql);
            var textReader = new StreamReader(stream);

            // act
            var fragment = fragmentBuilder.GetFragment(textReader, out var errors);

            // assert
            // Compatibility level 130+ should support CREATE OR ALTER
            Assert.IsNotNull(fragment, $"{description}: Fragment should not be null");
            Assert.IsEmpty(errors, $"{description}: No parsing errors should occur. Errors: {string.Join(", ", errors)}");
        }

        [TestCase("CREATE OR ALTER PROCEDURE with compatibility level 120", CreateOrAlterProcedureSql, 120)]
        [Ignore("Known limitation: ScriptDom parser returns null for CREATE OR ALTER with compatibility level < 130. See issue #337")]
        public void GetFragment_CreateOrAlterSyntax_LowerCompatibilityLevel_KnownLimitation(
            string description,
            string sql,
            int compatibilityLevel)
        {
            // arrange
            var fragmentBuilder = new FragmentBuilder(compatibilityLevel);
            var stream = ParsingUtility.GenerateStreamFromString(sql);
            var textReader = new StreamReader(stream);

            // act
            var fragment = fragmentBuilder.GetFragment(textReader, out var errors);

            // assert
            // This test documents a known limitation: lower compatibility levels do not support CREATE OR ALTER
            Assert.IsNull(fragment, $"{description}: Known limitation - parser returns null for unsupported syntax");
        }

        [TestCase("CREATE OR ALTER PROCEDURE with SET statements", CreateOrAlterWithSetStatementsSql)]
        [Ignore("Known limitation: ScriptDom parser returns null for CREATE OR ALTER combined with SET statements. See issue #337")]
        public void GetFragment_CreateOrAlterWithSetStatements_KnownLimitation(string description, string sql)
        {
            // arrange
            var fragmentBuilder = new FragmentBuilder(150);
            var stream = ParsingUtility.GenerateStreamFromString(sql);
            var textReader = new StreamReader(stream);

            // act
            var fragment = fragmentBuilder.GetFragment(textReader, out var errors);

            // assert
            // This test documents a known limitation: CREATE OR ALTER combined with SET statements fails to parse
            Assert.IsNull(fragment, $"{description}: Known limitation - parser fails with SET statements + CREATE OR ALTER");
        }

        [TestCase("Simple CREATE PROCEDURE", "CREATE PROCEDURE dbo.Test AS SELECT 1;")]
        [TestCase("Simple SELECT", "SELECT 1;")]
        public void GetFragment_StandardSyntax_ShouldParseWithoutErrors(string description, string sql)
        {
            // arrange
            var fragmentBuilder = new FragmentBuilder(120);
            var stream = ParsingUtility.GenerateStreamFromString(sql);
            var textReader = new StreamReader(stream);

            // act
            var fragment = fragmentBuilder.GetFragment(textReader, out var errors);

            // assert
            Assert.IsNotNull(fragment, $"{description}: Fragment should not be null");
            Assert.IsEmpty(errors, $"{description}: No parsing errors should occur");
            Assert.AreNotEqual(-1, fragment.FirstTokenIndex, $"{description}: Fragment should have valid token index");
        }
    }
}
