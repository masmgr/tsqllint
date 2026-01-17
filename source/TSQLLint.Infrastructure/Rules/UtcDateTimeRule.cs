using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using TSQLLint.Core.Interfaces;
using TSQLLint.Infrastructure.Rules.Common;

namespace TSQLLint.Infrastructure.Rules
{
    public class UtcDateTimeRule : BaseRuleVisitor, ISqlRule
    {
        private static readonly HashSet<string> LocalDateTimeFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CURRENT_TIMESTAMP",
            "GETDATE",
            "SYSDATETIME",
            "SYSDATETIMEOFFSET"
        };

        public UtcDateTimeRule(Action<string, string, int, int> errorCallback)
            : base(errorCallback)
        {
        }

        public override string RULE_NAME => "utc-datetime";

        public override string RULE_TEXT => "Avoid local date/time functions (GETDATE, SYSDATETIME, CURRENT_TIMESTAMP); use UTC equivalents.";

        public override void Visit(FunctionCall node)
        {
            var functionName = node.FunctionName?.Value;
            if (functionName == null || !LocalDateTimeFunctions.Contains(functionName))
            {
                return;
            }

            errorCallback(RULE_NAME, RULE_TEXT, GetLineNumber(node), GetColumnNumber(node));
        }
    }
}
