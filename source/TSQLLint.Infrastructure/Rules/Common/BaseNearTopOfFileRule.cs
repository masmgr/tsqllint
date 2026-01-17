using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;
using TSQLLint.Common;
using TSQLLint.Core.Interfaces;
using TSQLLint.Infrastructure.Rules.Common;

namespace TSQLLint.Infrastructure.Rules
{
    public abstract class BaseNearTopOfFileRule : BaseRuleVisitor, ISqlRule
    {
        private static readonly TSqlTokenType[] BeforeSet = new[] {
            TSqlTokenType.SingleLineComment,
            TSqlTokenType.MultilineComment,
            TSqlTokenType.WhiteSpace
        };

        public BaseNearTopOfFileRule(Action<string, string, int, int> errorCallback)
            : base(errorCallback)
        {
        }

        public abstract string Insert { get; }
        public abstract Func<string, bool> Remove { get; }

        public override void FixViolation(List<string> fileLines, IRuleViolation ruleViolation, FileLineActions actions)
        {
            try
            {
                var node = FixHelpers.FindNodes<TSqlScript>(fileLines).First();

                int index;
                for (index = node.FirstTokenIndex; index <= node.LastTokenIndex; index++)
                {
                    var token = node.ScriptTokenStream[index];
                    var tokenType = token.TokenType;

                    if (!BeforeSet.Contains(tokenType))
                    {
                        break;
                    }
                }

                actions.RemoveAll(Remove);
                actions.Insert(node.ScriptTokenStream[index].Line - 1, Insert);
            }
            catch (Exception ex) when (ex.Message.Contains("Parsing failed") || ex.Message.Contains("Incorrect syntax"))
            {
                // FixHelpers.FindNodes failed due to parsing errors
                // This can happen with newer T-SQL syntax (e.g., CREATE OR ALTER) that may not be fully supported
                // Skip the fix and continue - the violation will remain but won't crash the application
                System.Diagnostics.Debug.WriteLine(
                    $"Warning: Cannot fix '{ruleViolation.RuleName}' violation at line {ruleViolation.Line}. " +
                    $"This may be due to newer T-SQL syntax (e.g., CREATE OR ALTER) that is not fully supported by the fix feature. " +
                    $"Error: {ex.Message}");
            }
        }
    }
}
