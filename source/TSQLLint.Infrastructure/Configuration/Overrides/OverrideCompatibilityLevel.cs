using TSQLLint.Core;
using TSQLLint.Core.Interfaces;

namespace TSQLLint.Infrastructure.Configuration.Overrides
{
    public class OverrideCompatibilityLevel : IOverride
    {
        public OverrideCompatibilityLevel(string value)
        {
            if (int.TryParse(value, out var parsedCompatibilityLevel))
            {
                CompatibilityLevel =
                    Core.CompatibilityLevel.Validate(parsedCompatibilityLevel);
            }
            else
            {
                CompatibilityLevel = Constants.DefaultCompatibilityLevel;
            }
        }

        public int CompatibilityLevel { get; }
    }
}
