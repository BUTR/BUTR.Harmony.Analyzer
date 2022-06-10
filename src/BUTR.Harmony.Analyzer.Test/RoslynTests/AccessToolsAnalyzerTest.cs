using BUTR.Harmony.Analyzer.Analyzers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading.Tasks;

using TestHelper;

namespace BUTR.Harmony.Analyzer.Test.Roslyn
{
    [TestClass]
    public partial class AccessToolsAnalyzerTest : BaseTest
    {
        private static ProjectBuilder CreateProjectBuilder() => new ProjectBuilder()
            .WithAnalyzer<AccessToolsAnalyzer>()
            .AddSystemTextJson();
    }
}