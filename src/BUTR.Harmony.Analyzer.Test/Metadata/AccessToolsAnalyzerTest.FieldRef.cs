using HarmonyLib;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Numerics;
using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test.Metadata
{
    public partial class AccessToolsAnalyzerTest
    {
        private static string SourceCode(string method, bool isCorrect, string objectType, string fieldType, string fieldName) => @$"
{HarmonyBase}

namespace BUTR.Harmony.Analyzer.Test
{{
    class TestProgram
    {{
        public static void TestMethod()
        {{
            {(isCorrect ? "" : "[||]")}HarmonyLib.AccessTools.{method}<{objectType}, {fieldType}>(""{fieldName}"");
        }}
    }}
}}
";

        ///

        [DataTestMethod]
        [DataRow("StaticFieldRefAccess", true, "System.Text.Json.JsonException", "string", "_message")]
        [DataRow("StructFieldRefAccess", true, "System.Numerics.Vector3", "float", "X")]
        public async Task FieldRef_Default(string method, bool isCorrect, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("StaticFieldRefAccess", false, "System.Text.Json.JsonException", "string", "_message1")]
        [DataRow("StructFieldRefAccess", false, "System.Numerics.Vector3", "float", "X1")]

        public async Task FieldRef_Member(string method, bool isCorrect, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("StaticFieldRefAccess", false, "System.Text.Json.JsonException", "bool", "_message")]
        [DataRow("StructFieldRefAccess", false, "System.Numerics.Vector3", "int", "X")]
        public async Task FieldRef_MemberTypePrimitive(string method, bool isCorrect, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("StaticFieldRefAccess", false, "System.Text.Json.Nodes.JsonObject", "System.Collections.Generic.List<string>", "_dictionary")]
        [DataRow("StructFieldRefAccess", false, "System.Numerics.Vector3", "System.Collections.Generic.List<string>", "X")]
        public async Task FieldRef_MemberTypeObject(string method, bool isCorrect, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, objectType, fieldType, fieldName)).ValidateAsync();
        }
    }
}