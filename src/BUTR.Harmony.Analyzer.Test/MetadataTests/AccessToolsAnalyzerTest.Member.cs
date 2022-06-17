using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test.Metadata
{
    public partial class AccessToolsAnalyzerTest
    {
        public enum MemberTestType { TypeOf, String, TypeOfOnly, StringOnly }

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
        [DataRow("Field", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "_message")]
        [DataRow("Field", true, MemberTestType.String, "System.Text.Json.JsonException", "_message")]
        [DataRow("DeclaredField", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "_message")]
        [DataRow("DeclaredField", true, MemberTestType.String, "System.Text.Json.JsonException", "_message")]
        public async Task Field_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Field", false, MemberTestType.TypeOf, "System.Text.Json.JsonException", "_message1")]
        [DataRow("Field", false, MemberTestType.String, "System.Text.Json.JsonException", "_message1")]
        [DataRow("DeclaredField", false, MemberTestType.TypeOf, "System.Text.Json.JsonException", "_message1")]
        [DataRow("DeclaredField", false, MemberTestType.String, "System.Text.Json.JsonException", "_message1")]
        public async Task Field_Member(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Field", true, MemberTestType.TypeOf, "System.Text.Json.Nodes.JsonArray", "_parent")]
        [DataRow("Field", true, MemberTestType.String, "System.Text.Json.Nodes.JsonArray", "_parent")]
        [DataRow("DeclaredField", false, MemberTestType.TypeOf, "System.Text.Json.Nodes.JsonArray", "_parent")]
        [DataRow("DeclaredField", false, MemberTestType.String, "System.Text.Json.Nodes.JsonArray", "_parent")]
        public async Task Field_MemberFromBase(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Field", false, MemberTestType.String, "NonExistingType", "NonExistingField")]
        [DataRow("DeclaredField", false, MemberTestType.String, "NonExistingType", "NonExistingField")]
        public async Task Field_Type(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        ///

        [DataTestMethod]
        [DataRow("Property", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "AppendPathInformation")]
        [DataRow("Property", true, MemberTestType.String, "System.Text.Json.JsonException", "AppendPathInformation")]
        [DataRow("DeclaredProperty", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "AppendPathInformation")]
        [DataRow("DeclaredProperty", true, MemberTestType.String, "System.Text.Json.JsonException", "AppendPathInformation")]
        public async Task Property_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Property", false, MemberTestType.TypeOf, "System.Text.Json.JsonException", "AppendPathInformation1")]
        [DataRow("Property", false, MemberTestType.String, "System.Text.Json.JsonException", "AppendPathInformation1")]
        [DataRow("DeclaredProperty", false, MemberTestType.TypeOf, "System.Text.Json.JsonException", "AppendPathInformation1")]
        [DataRow("DeclaredProperty", false, MemberTestType.String, "System.Text.Json.JsonException", "AppendPathInformation1")]
        public async Task Property_Member(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Property", true, MemberTestType.TypeOf, "System.Text.Json.Nodes.JsonArray", "Root")]
        [DataRow("Property", true, MemberTestType.String, "System.Text.Json.Nodes.JsonArray", "Root")]
        [DataRow("DeclaredProperty", false, MemberTestType.TypeOf, "System.Text.Json.Nodes.JsonArray", "Root")]
        [DataRow("DeclaredProperty", false, MemberTestType.String, "System.Text.Json.Nodes.JsonArray", "Root")]
        public async Task Property_MemberFromBase(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Property", false, MemberTestType.String, "NonExistingType", "NonExistingProperty")]
        [DataRow("DeclaredProperty", false, MemberTestType.String, "NonExistingType", "NonExistingProperty")]
        public async Task Property_Type(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        ///

        [DataTestMethod]
        [DataRow("PropertyGetter", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "AppendPathInformation")]
        [DataRow("PropertyGetter", true, MemberTestType.String, "System.Text.Json.JsonException", "AppendPathInformation")]
        [DataRow("DeclaredPropertyGetter", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "AppendPathInformation")]
        [DataRow("DeclaredPropertyGetter", true, MemberTestType.String, "System.Text.Json.JsonException", "AppendPathInformation")]
        public async Task PropertyGetter_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("PropertyGetter", false, MemberTestType.String, "NonExistingType", "NonExistingProperty")]
        [DataRow("DeclaredPropertyGetter", false, MemberTestType.String, "NonExistingType", "NonExistingProperty")]
        public async Task PropertyGetter_Type(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        ///

        [DataTestMethod]
        [DataRow("PropertySetter", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "AppendPathInformation")]
        [DataRow("PropertySetter", true, MemberTestType.String, "System.Text.Json.JsonException", "AppendPathInformation")]
        [DataRow("DeclaredPropertySetter", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "AppendPathInformation")]
        [DataRow("DeclaredPropertySetter", true, MemberTestType.String, "System.Text.Json.JsonException", "AppendPathInformation")]
        public async Task PropertySetter_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("PropertySetter", false, MemberTestType.TypeOf, "System.Text.Json.JsonException", "Message")]
        [DataRow("PropertySetter", false, MemberTestType.String, "System.Text.Json.JsonException", "Message")]
        [DataRow("DeclaredPropertySetter", false, MemberTestType.TypeOf, "System.Text.Json.JsonException", "Message")]
        [DataRow("DeclaredPropertySetter", false, MemberTestType.String, "System.Text.Json.JsonException", "Message")]
        public async Task PropertySetter_Missing(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("PropertySetter", false, MemberTestType.String, "NonExistingType", "NonExistingProperty")]
        [DataRow("DeclaredPropertySetter", false, MemberTestType.String, "NonExistingType", "NonExistingProperty")]
        public async Task PropertySetter_Type(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        ///

        [DataTestMethod]
        [DataRow("Method", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "SetMessage")]
        [DataRow("Method", true, MemberTestType.String, "System.Text.Json.JsonException", "SetMessage")]
        [DataRow("DeclaredMethod", true, MemberTestType.TypeOf, "System.Text.Json.JsonException", "SetMessage")]
        [DataRow("DeclaredMethod", true, MemberTestType.String, "System.Text.Json.JsonException", "SetMessage")]
        public async Task Method_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Method", false, MemberTestType.TypeOf, "System.Text.Json.JsonException", "SetMessage1")]
        [DataRow("Method", false, MemberTestType.String, "System.Text.Json.JsonException", "SetMessage1")]
        [DataRow("DeclaredMethod", false, MemberTestType.TypeOf, "System.Text.Json.JsonException", "SetMessage1")]
        [DataRow("DeclaredMethod", false, MemberTestType.String, "System.Text.Json.JsonException", "SetMessage1")]
        public async Task Method_Member(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Method", true, MemberTestType.TypeOf, "System.Text.Json.Nodes.JsonArray", "AssignParent")]
        [DataRow("Method", true, MemberTestType.String, "System.Text.Json.Nodes.JsonArray", "AssignParent")]
        [DataRow("DeclaredMethod", false, MemberTestType.TypeOf, "System.Text.Json.Nodes.JsonArray", "AssignParent")]
        [DataRow("DeclaredMethod", false, MemberTestType.String, "System.Text.Json.Nodes.JsonArray", "AssignParent")]
        public async Task Method_MemberFromBase(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Method", false, MemberTestType.String, "NonExistingType", "NonExistingMethod")]
        [DataRow("DeclaredMethod", false, MemberTestType.String, "NonExistingType", "NonExistingMethod")]
        public async Task Method_Type(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        ///

        [DataTestMethod]
        [DataRow("Constructor", true, MemberTestType.TypeOfOnly, "System.Text.Json.JsonException", "typeof(string)")]
        [DataRow("DeclaredConstructor", true, MemberTestType.TypeOfOnly, "System.Text.Json.JsonException", "typeof(string)")]
        [DataRow("DeclaredConstructor", true, MemberTestType.StringOnly, "System.Text.Json.JsonException", "typeof(string)")]
        [DataRow("DeclaredConstructor", false, MemberTestType.TypeOfOnly, "System.Text.Json.JsonException", "typeof(int)")]
        [DataRow("DeclaredConstructor", false, MemberTestType.StringOnly, "System.Text.Json.JsonException", "typeof(int)")]
        public async Task Constructor_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("DeclaredConstructor", false, MemberTestType.TypeOfOnly, "System.Text.Json.Nodes.JsonArray", "typeof(string)")]
        [DataRow("DeclaredConstructor", false, MemberTestType.StringOnly, "System.Text.Json.Nodes.JsonArray", "typeof(string)")]
        public async Task Constructor_MemberFromBase(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("DeclaredConstructor", false, MemberTestType.StringOnly, "NonExistingType", "typeof(int)")]
        public async Task Constructor_Type(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }
    }
}