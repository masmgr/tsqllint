using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using TSQLLint.Core.Interfaces;
using TSQLLint.Infrastructure.Configuration.Overrides;

namespace TSQLLint.Tests.Helpers.ObjectComparers
{
    [ExcludeFromCodeCoverage]
    public class OverrideComparer : IComparer, IComparer<IOverride>
    {
        public int Compare(object x, object y)
        {
            if (!(x is IOverride lhs) || !(y is IOverride rhs))
            {
                throw new InvalidOperationException("cannot compare null object");
            }

            return Compare(lhs, rhs);
        }

        public int Compare(IOverride x, IOverride y)
        {
            if (x.GetType() == typeof(OverrideCompatibilityLevel) && y.GetType() == typeof(OverrideCompatibilityLevel))
            {
                if (x is OverrideCompatibilityLevel lhs && y is OverrideCompatibilityLevel rhs && lhs.CompatibilityLevel != rhs.CompatibilityLevel)
                {
                    return -1;
                }
            }

            return 0;
        }
    }
}
