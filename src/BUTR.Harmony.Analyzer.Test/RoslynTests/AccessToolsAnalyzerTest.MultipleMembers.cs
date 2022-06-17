using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test.Roslyn
{
    public partial class AccessToolsAnalyzerTest
    {
        private static string SourceCodeMultiple(string content) => @$"
{HarmonyBase}

namespace BUTR.Harmony.Analyzer.Test
{{
    class BaseTestClass
    {{
        private string _baseField;
        private bool _baseProperty {{ get; set; }}
        private int _baseMethod() {{ return 1; }}
        private int _baseMethod(string str) {{ return 1; }}
        protected BaseTestClass(string str) {{ }}
    }}

    class TestClass : BaseTestClass
    {{
        private string _field;
        private bool _property {{ get; set; }}
        private bool _prop;
        private bool _propertyGet => _prop;
        private bool _propertySet {{ set {{ _prop = value; }} }}
        private int _method() {{ return 1; }}
        private int _method(string str) {{ return 1; }}
        private TestClass(char c) : base(c.ToString()) {{ }}
    }}

    class TestProgram
    {{
        public static void TestMethod()
        {{
{content}
        }}
    }}
}}
";

        private static string ValidMulti(string method, MemberTestType testType, string type, string member) => @$"
            var variable = HarmonyLib.AccessTools.{method}({GetArg(testType, type, member)}) ??
                           HarmonyLib.AccessTools.{method}({GetArg(testType, type, member)}) ??
                           HarmonyLib.AccessTools.{method}({GetArg(testType, type, member)});";
        private static string PartialMulti(string method, MemberTestType testType, string type, string member) => @$"
            var variable = HarmonyLib.AccessTools.{method}({GetArg(testType, type, member)}) ??
                           HarmonyLib.AccessTools.{method}({GetArg(testType, type, $"{member}2")}) ??
                           HarmonyLib.AccessTools.{method}({GetArg(testType, type, $"{member}3")});";
        private static string InvalidMulti(string method, MemberTestType testType, string type, string member) => @$"
            var variable = [||]HarmonyLib.AccessTools.{method}({GetArg(testType, type, $"{member}1")}) ??
                           [||]HarmonyLib.AccessTools.{method}({GetArg(testType, type, $"{member}2")}) ??
                           [||]HarmonyLib.AccessTools.{method}({GetArg(testType, type, $"{member}3")});";

        ///

        [TestMethod]
        [DataRow("Field", MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_field")]
        [DataRow("Field", MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_field")]
        [DataRow("DeclaredField", MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_field")]
        [DataRow("DeclaredField", MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_field")]
        public async Task Multiple_Field(string method, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCodeMultiple(ValidMulti(method, testType, type, member))).ValidateAsync();
            await CreateProjectBuilder().WithSourceCode(SourceCodeMultiple(PartialMulti(method, testType, type, member))).ValidateAsync();
            await CreateProjectBuilder().WithSourceCode(SourceCodeMultiple(InvalidMulti(method, testType, type, member))).ValidateAsync();
        }

        [TestMethod]
        [DataRow("Property", MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("Property", MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("DeclaredProperty", MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("DeclaredProperty", MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        public async Task Multiple_Property(string method, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCodeMultiple(ValidMulti(method, testType, type, member))).ValidateAsync();
            await CreateProjectBuilder().WithSourceCode(SourceCodeMultiple(PartialMulti(method, testType, type, member))).ValidateAsync();
            await CreateProjectBuilder().WithSourceCode(SourceCodeMultiple(InvalidMulti(method, testType, type, member))).ValidateAsync();
        }

        [TestMethod]
        [DataRow("Method", MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_method")]
        [DataRow("Method", MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_method")]
        [DataRow("DeclaredMethod", MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_method")]
        [DataRow("DeclaredMethod", MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_method")]
        public async Task Multiple_Method(string method, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCodeMultiple(ValidMulti(method, testType, type, member))).ValidateAsync();
            await CreateProjectBuilder().WithSourceCode(SourceCodeMultiple(PartialMulti(method, testType, type, member))).ValidateAsync();
            await CreateProjectBuilder().WithSourceCode(SourceCodeMultiple(InvalidMulti(method, testType, type, member))).ValidateAsync();
        }
    }
}