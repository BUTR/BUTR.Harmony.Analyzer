using BUTR.Harmony.Analyzer.Analyzers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestHelper;

namespace BUTR.Harmony.Analyzer.Test.Metadata
{
    [TestClass]
    public partial class AccessToolsAnalyzerTest : BaseTest
    {
        private static ProjectBuilder CreateProjectBuilder() => new ProjectBuilder()
            .WithAnalyzer<ExistenceAnalyzer>()
            .AddSystemTextJson();
    }
}