using System.Collections.Generic;

namespace TSQLLint.Core
{
    public static class CompatibilityLevel
    {
        public static int Validate(int compatibilityLevel)
        {
            var validCompatibilityLevels = new List<int> { 80, 90, 100, 110, 120, 130, 140, 150 };
            return validCompatibilityLevels.Contains(compatibilityLevel)
                ? compatibilityLevel
                : Constants.DefaultCompatibilityLevel;
        }
    }
}
