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

        [TestMethod]
        [DataRow("_baseProperty", true)]
        [DataRow("_baseProperty1", false)]
        public async Task Generic_Default(string method, bool isCorrect)
        {
            await CreateProjectBuilder().WithSourceCode($@"
{HarmonyBase}

namespace BUTR.Harmony.Analyzer.Test
{{
    class TestClassBase
    {{
        private bool _baseProperty {{ get; set; }}
    }}

    class TestClass<TSelectorItemVM> : TestClassBase where TSelectorItemVM : TestClassBase
    {{
        public static void TestMethod()
        {{
            {(isCorrect ? "" : "[||]")}HarmonyLib.AccessTools.Property(typeof(TSelectorItemVM), ""{method}"");
        }}
    }}
}}
").ValidateAsync();
        }
    }
}