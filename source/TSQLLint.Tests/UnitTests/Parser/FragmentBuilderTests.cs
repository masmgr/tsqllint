using System.IO;
using NUnit.Framework;
using TSQLLint.Infrastructure.Parser;

namespace TSQLLint.Tests.UnitTests.Parser
{
    [TestFixture]
    public class FragmentBuilderTests
    {
        [Test]
        public void CreateOrAlterProcedure_ParsesWithoutErrors()
        {
            var builder = new FragmentBuilder(150);
            using var reader = new StringReader("CREATE OR ALTER PROCEDURE dbo.Test AS SELECT 1;");

            var fragment = builder.GetFragment(reader, out var errors);

            Assert.NotNull(fragment);
            Assert.IsEmpty(errors);
        }
    }
}
