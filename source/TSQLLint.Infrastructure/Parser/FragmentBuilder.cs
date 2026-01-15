using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public FragmentBuilder() : this(Constants.DefaultCompatabilityLevel)
        {
        }

        public FragmentBuilder(int compatabilityLevel)
        {
            parser = GetSqlParser(CompatabilityLevel.Validate(compatabilityLevel));
        }

        public TSqlFragment GetFragment(TextReader txtRdr, out IList<ParseError> errors, IEnumerable<IOverride> overrides = null)
        {
            TSqlFragment fragment;

            OverrideCompatabilityLevel compatibilityLevel = null;
            if (overrides != null)
            {
                foreach (var lintingOverride in overrides)
                {
                    if (lintingOverride is OverrideCompatabilityLevel overrideCompatability)
                    {
                        compatibilityLevel = overrideCompatability;
                    }
                }
            }

            if (compatibilityLevel != null )
            {
                var tempParser = GetSqlParser(compatibilityLevel.CompatabilityLevel);
                fragment = tempParser.Parse(txtRdr, out errors);
                return fragment?.FirstTokenIndex != -1 ? fragment : null;
            }

            fragment = parser.Parse(txtRdr, out errors);
            return fragment?.FirstTokenIndex != -1 ? fragment : null;
        }

        private static TSqlParser GetSqlParser(int compatabilityLevel)
        {
            compatabilityLevel = CompatabilityLevel.Validate(compatabilityLevel);

            var availableVersions = GetAvailableParserVersions();
            if (availableVersions.Count == 0)
            {
                return new TSql120Parser(true);
            }

            var bestVersion = GetBestParserVersion(availableVersions, compatabilityLevel);
            var parserType = typeof(TSqlParser).Assembly.GetType(GetParserTypeName(bestVersion));
            if (parserType == null)
            {
                return new TSql120Parser(true);
            }

            return (TSqlParser)Activator.CreateInstance(parserType, new object[] { true });
        }

        private static IReadOnlyList<int> GetAvailableParserVersions()
        {
            var versions = new List<int>();
            var assembly = typeof(TSqlParser).Assembly;
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(type => type != null).ToArray();
            }

            foreach (var type in types)
            {
                var name = type.Name;
                if (!name.StartsWith("TSql", StringComparison.Ordinal) || !name.EndsWith("Parser", StringComparison.Ordinal))
                {
                    continue;
                }

                var versionText = name.Substring(4, name.Length - 4 - "Parser".Length);
                if (int.TryParse(versionText, out var version))
                {
                    versions.Add(version);
                }
            }

            return versions.Distinct().OrderBy(x => x).ToList();
        }

        private static int GetBestParserVersion(IReadOnlyList<int> availableVersions, int compatabilityLevel)
        {
            var bestMatch = availableVersions.LastOrDefault(x => x <= compatabilityLevel);
            return bestMatch != 0 ? bestMatch : availableVersions.Last();
        }

        private static string GetParserTypeName(int compatabilityLevel)
        {
            return string.Format("Microsoft.SqlServer.TransactSql.ScriptDom.TSql{0}Parser", compatabilityLevel);
        }
    }
}
