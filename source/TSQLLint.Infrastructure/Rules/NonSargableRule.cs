using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using TSQLLint.Core.Interfaces;
using TSQLLint.Infrastructure.Rules.Common;

namespace TSQLLint.Infrastructure.Rules
{
    public class NonSargableRule : BaseRuleVisitor, ISqlRule
    {
        private static readonly HashSet<string> DateFunctions = new(StringComparer.OrdinalIgnoreCase)
        {
            "DATEADD",
            "DATEDIFF",
            "DATEDIFF_BIG",
            "DATENAME",
            "DATEPART",
            "DATETRUNC",
            "DATE_BUCKET"
        };

        private readonly List<TSqlFragment> errorsReported = new();

        public NonSargableRule(Action<string, string, int, int> errorCallback)
            : base(errorCallback)
        {
        }

        public override string RULE_NAME => "non-sargable";

        public override string RULE_TEXT => "Performing functions on filter clauses or join predicates can cause performance problems";

        public override void Visit(JoinTableReference node)
        {
            var predicateExpressionVisitor = new PredicateVisitor();
            node.AcceptChildren(predicateExpressionVisitor);
            var multiClauseQuery = predicateExpressionVisitor.PredicatesFound;

            var joinVisitor = new JoinQueryVisitor(VisitorCallback, multiClauseQuery);
            node.AcceptChildren(joinVisitor);
        }

        public override void Visit(WhereClause node)
        {
            var predicateExpressionVisitor = new PredicateVisitor();
            node.Accept(predicateExpressionVisitor);
            var multiClauseQuery = predicateExpressionVisitor.PredicatesFound;

            var childVisitor = new FunctionVisitor(VisitorCallback, multiClauseQuery);
            node.Accept(childVisitor);
        }

        private void VisitorCallback(TSqlFragment childNode)
        {
            if (errorsReported.Contains(childNode))
            {
                return;
            }

            var dynamicSqlColumnAdjustment = GetDynamicSqlColumnOffset(childNode);

            errorsReported.Add(childNode);
            errorCallback(RULE_NAME, RULE_TEXT, GetLineNumber(childNode), ColumnNumberCalculator.GetNodeColumnPosition(childNode) + dynamicSqlColumnAdjustment);
        }

        private class JoinQueryVisitor : TSqlFragmentVisitor
        {
            private readonly Action<TSqlFragment> childCallback;
            private readonly bool isMultiClauseQuery;

            public JoinQueryVisitor(Action<TSqlFragment> childCallback, bool multiClauseQuery)
            {
                this.childCallback = childCallback;
                isMultiClauseQuery = multiClauseQuery;
            }

            public override void Visit(BooleanComparisonExpression node)
            {
                var childVisitor = new FunctionVisitor(childCallback, isMultiClauseQuery);
                node.Accept(childVisitor);
            }
        }

        private class PredicateVisitor : TSqlFragmentVisitor
        {
            public bool PredicatesFound { get; private set; }

            public override void Visit(BooleanBinaryExpression node)
            {
                PredicatesFound = true;
            }
        }

        private class FunctionVisitor : TSqlFragmentVisitor
        {
            private readonly bool isMultiClause;
            private readonly Action<TSqlFragment> childCallback;

            public FunctionVisitor(Action<TSqlFragment> errorCallback, bool isMultiClause)
            {
                childCallback = errorCallback;
                this.isMultiClause = isMultiClause;
            }

            public override void Visit(FunctionCall node)
            {
                var functionName = node.FunctionName?.Value;
                if (string.IsNullOrWhiteSpace(functionName))
                {
                    return;
                }

                if (functionName.Equals("ISNULL", StringComparison.OrdinalIgnoreCase) && isMultiClause)
                {
                    return;
                }

                var skipDatePartParameter = DateFunctions.Contains(functionName);
                FindColumnReferences(node, skipDatePartParameter);
            }

            public override void Visit(LeftFunctionCall node)
            {
                FindColumnReferences(node, false);
            }

            public override void Visit(RightFunctionCall node)
            {
                FindColumnReferences(node, false);
            }

            public override void Visit(ConvertCall node)
            {
                FindColumnReferences(node, false);
            }

            public override void Visit(CastCall node)
            {
                FindColumnReferences(node, false);
            }

            private void FindColumnReferences(TSqlFragment node, bool skipDatePartParameter)
            {
                var columnReferenceVisitor = new ColumnReferenceVisitor();

                if (skipDatePartParameter && node is FunctionCall functionCall)
                {
                    for (var i = 1; i < functionCall.Parameters.Count; i++)
                    {
                        functionCall.Parameters[i].Accept(columnReferenceVisitor);
                    }
                }
                else
                {
                    node.AcceptChildren(columnReferenceVisitor);
                }

                if (columnReferenceVisitor.ColumnReferenceFound)
                {
                    childCallback(node);
                }
            }
        }

        private class ColumnReferenceVisitor : TSqlFragmentVisitor
        {
            public bool ColumnReferenceFound { get; private set; }

            public override void Visit(ColumnReferenceExpression node)
            {
                ColumnReferenceFound = true;
            }

            public override void ExplicitVisit(FunctionCall node)
            {
                if (ColumnReferenceFound)
                {
                    return;
                }

                var functionName = node.FunctionName?.Value;
                if (!string.IsNullOrWhiteSpace(functionName) && DateFunctions.Contains(functionName))
                {
                    for (var i = 1; i < node.Parameters.Count; i++)
                    {
                        node.Parameters[i].Accept(this);
                        if (ColumnReferenceFound)
                        {
                            return;
                        }
                    }

                    return;
                }

                node.AcceptChildren(this);
            }
        }
    }
}
