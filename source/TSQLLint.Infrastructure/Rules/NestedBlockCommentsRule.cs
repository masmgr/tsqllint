using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using TSQLLint.Core.Interfaces;
using TSQLLint.Infrastructure.Rules.Common;

namespace TSQLLint.Infrastructure.Rules
{
    public class NestedBlockCommentsRule : BaseRuleVisitor, ISqlRule
    {
        public NestedBlockCommentsRule(Action<string, string, int, int> errorCallback)
            : base(errorCallback)
        {
        }

        public override string RULE_NAME => "nested-block-comments";

        public override string RULE_TEXT => "Nested block comments can cause parsing errors in tools that do not support them";

        public override void Visit(TSqlScript node)
        {
            if (node?.ScriptTokenStream == null)
            {
                return;
            }

            for (var index = 0; index <= node.LastTokenIndex; index++)
            {
                var token = node.ScriptTokenStream[index];

                if (token.TokenType != TSqlTokenType.MultilineComment)
                {
                    continue;
                }

                if (TryGetNestedCommentPosition(token, out var line, out var column))
                {
                    errorCallback(RULE_NAME, RULE_TEXT, line, column);
                }
            }
        }

        private int GetColumnOffsetForNestedComment(TSqlParserToken token, int index, int lastLineStartIndex, int lineOffset)
        {
            if (lineOffset == 0)
            {
                return GetColumnNumber(token) + index;
            }

            return index - lastLineStartIndex + 1;
        }

        private int GetLineOffsetForNestedComment(string commentText, int index, out int lastLineStartIndex)
        {
            var lineOffset = 0;
            lastLineStartIndex = 0;

            for (var i = 0; i < index; i++)
            {
                if (commentText[i] == '\n')
                {
                    lineOffset++;
                    lastLineStartIndex = i + 1;
                }
            }

            return lineOffset;
        }

        private bool TryGetNestedCommentPosition(TSqlParserToken token, out int line, out int column)
        {
            line = 0;
            column = 0;

            if (string.IsNullOrEmpty(token.Text))
            {
                return false;
            }

            var index = token.Text.IndexOf("/*", 2, StringComparison.Ordinal);
            if (index < 0)
            {
                return false;
            }

            var lineOffset = GetLineOffsetForNestedComment(token.Text, index, out var lastLineStartIndex);
            line = GetLineNumber(token) + lineOffset;
            column = GetColumnOffsetForNestedComment(token, index, lastLineStartIndex, lineOffset);

            return true;
        }
    }
}
