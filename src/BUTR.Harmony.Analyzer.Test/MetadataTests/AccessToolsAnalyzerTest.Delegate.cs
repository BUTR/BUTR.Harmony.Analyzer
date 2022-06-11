using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test.Metadata
{
    public partial class AccessToolsAnalyzerTest
    {
        /*
        private static string SourceCode(string method, bool isCorrect, MemberTestType testType, string type, string member) => @$"
{HarmonyBase}

namespace BUTR.Harmony.Analyzer.Test
{{
    class TestProgram
    {{
        public static void TestMethod()
        {{
            {(isCorrect ? "" : "[||]")}HarmonyLib.AccessTools.{method}({GetArg(testType, type, member)});
        }}
    }}
}}
";

        private static string GetArg(MemberTestType testType, string type, string member) => testType switch
        {
            MemberTestType.TypeOf => $"typeof({type}), \"{member}\"",
            MemberTestType.String => $"\"{type}:{member}\"",
            MemberTestType.TypeOfOnly => $"typeof({type}), new System.Type[] {{ {member} }}",
            MemberTestType.StringOnly => $"\"{type}\", new System.Type[] {{ {member} }}",
            _ => ""
        };

        ///

        [DataTestMethod]
        [DataRow("Delegate", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "_message")]
        [DataRow("Field", true, MemberTestType.String, "System.Text.Json.JsonException", "_message")]
        [DataRow("DeclaredField", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "_message")]
        [DataRow("DeclaredField", true, MemberTestType.String, "System.Text.Json.JsonException", "_message")]
        public async Task Delegate_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Field", false, MemberTestType.TypeOf, "System.Text.Json.JsonException", "_message1")]
        [DataRow("Field", false, MemberTestType.String, "System.Text.Json.JsonException", "_message1")]
        [DataRow("DeclaredField", false, MemberTestType.TypeOf, "System.Text.Json.JsonException", "_message1")]
        [DataRow("DeclaredField", false, MemberTestType.String, "System.Text.Json.JsonException", "_message1")]
        public async Task Delegate_Member(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Field", true, MemberTestType.TypeOf, "System.Text.Json.Nodes.JsonArray", "_parent")]
        [DataRow("Field", true, MemberTestType.String, "System.Text.Json.Nodes.JsonArray", "_parent")]
        [DataRow("DeclaredField", false, MemberTestType.TypeOf, "System.Text.Json.Nodes.JsonArray", "_parent")]
        [DataRow("DeclaredField", false, MemberTestType.String, "System.Text.Json.Nodes.JsonArray", "_parent")]
        public async Task Delegate_MemberFromBase(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Field", false, MemberTestType.String, "NonExistingType", "NonExistingField")]
        [DataRow("DeclaredField", false, MemberTestType.String, "NonExistingType", "NonExistingField")]
        public async Task Delegate_Type(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        ///
        */
    }
}