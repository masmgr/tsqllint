using System.Collections.Generic;
using System.IO;
using TSQLLint.Common;

namespace TSQLLint.Core.Interfaces
{
    public interface IRuleVisitor
    {
        void VisitRules(string path, IEnumerable<IRuleException> ignoredRules, Stream sqlFileStream);
    }
}
