using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using TSQLLint.Core;
using TSQLLint.Core.Interfaces;
using TSQLLint.Infrastructure.Configuration.Overrides;
using TSQLLint.Infrastructure.Interfaces;

namespace TSQLLint.Infrastructure.Parser
{
    public class FragmentBuilder : IFragmentBuilder
    {
        private readonly TSqlParser parser;

        public FragmentBuilder() : this(Constants.DefaultCompatibilityLevel)
        {
        }

        public FragmentBuilder(int compatibilityLevel)
        {
            parser = GetSqlParser(CompatibilityLevel.Validate(compatibilityLevel));
        }

        public TSqlFragment GetFragment(TextReader txtRdr, out IList<ParseError> errors, IEnumerable<IOverride> overrides = null)
        {
            TSqlFragment fragment;

            OverrideCompatibilityLevel compatibilityLevel = null;
            if (overrides != null)
            {
                foreach (var lintingOverride in overrides)
                {
                    if (lintingOverride is OverrideCompatibilityLevel overrideCompatibility)
                    {
                        compatibilityLevel = overrideCompatibility;
                    }
                }
            }

            if (compatibilityLevel != null )
            {
                var tempParser = GetSqlParser(compatibilityLevel.CompatibilityLevel);
                fragment = tempParser.Parse(txtRdr, out errors);
                return fragment?.FirstTokenIndex != -1 ? fragment : null;
            }

            fragment = parser.Parse(txtRdr, out errors);
            return fragment?.FirstTokenIndex != -1 ? fragment : null;
        }

        private static TSqlParser GetSqlParser(int compatibilityLevel)
        {
            compatibilityLevel = CompatibilityLevel.Validate(compatibilityLevel);
            var fullyQualifiedName = string.Format("Microsoft.SqlServer.TransactSql.ScriptDom.TSql{0}Parser", compatibilityLevel);

            TSqlParser parser = null;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var parserType = asm.GetType(fullyQualifiedName);
                if (parserType != null)
                {
                    parser = (TSqlParser)Activator.CreateInstance(parserType, new object[] { true });
                    break;
                }
            }

            return parser ?? new TSql120Parser(true);
        }
    }
}
