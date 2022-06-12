using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test.Metadata
{
    public partial class AccessToolsAnalyzerTest
    {
        public enum FieldRefTestType { Type, String }
        
        private static string SourceCode(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName) => @$"
{HarmonyBase}

namespace BUTR.Harmony.Analyzer.Test
{{
    class TestProgram
    {{
        public static void TestMethod()
        {{
            {(testType switch   
            {
                FieldRefTestType.Type => @$"{(isCorrect ? "" : "[||]")}HarmonyLib.AccessTools.{method}<{objectType}, {fieldType}>(""{fieldName}"");",
                FieldRefTestType.String => @$"{(isCorrect ? "" : "[||]")}HarmonyLib.AccessTools.{method}<{fieldType}>(""{objectType}:{fieldName}"");"
            })}
        }}
    }}
}}
";

        ///
        /// 
        [DataTestMethod]
        [DataRow("FieldRefAccess", true, FieldRefTestType.Type, "System.Text.Json.JsonException", "string", "_message")]
        [DataRow("FieldRefAccess", true, FieldRefTestType.String, "System.Text.Json.JsonException", "string", "_message")]
        [DataRow("StaticFieldRefAccess", true, FieldRefTestType.Type, "System.Type", "char", "Delimiter")]
        [DataRow("StaticFieldRefAccess", true, FieldRefTestType.String, "System.Type", "char", "Delimiter")]
        [DataRow("StructFieldRefAccess", true, FieldRefTestType.Type, "System.Numerics.Vector3", "float", "X")]
        [DataRow("StructFieldRefAccess", true, FieldRefTestType.String, "System.Numerics.Vector3", "float", "X")]
        public async Task FieldRef_Default(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("FieldRefAccess", false, FieldRefTestType.Type, "System.Text.Json.JsonException", "string", "_message1")]
        [DataRow("FieldRefAccess", false, FieldRefTestType.String, "System.Text.Json.JsonException", "string", "_message1")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.Type, "System.Type", "char", "Delimiter1")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.String, "System.Type", "char", "Delimiter1")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.Type, "System.Numerics.Vector3", "float", "X1")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.String, "System.Numerics.Vector3", "float", "X1")]

        public async Task FieldRef_Member(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("FieldRefAccess", false, FieldRefTestType.Type, "System.Text.Json.JsonException", "bool", "_message")]
        [DataRow("FieldRefAccess", false, FieldRefTestType.String, "System.Text.Json.JsonException", "bool", "_message")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.Type, "System.Type", "bool", "Delimiter")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.String, "System.Type", "bool", "Delimiter")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.Type, "System.Numerics.Vector3", "int", "X")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.String, "System.Numerics.Vector3", "int", "X")]
        public async Task FieldRef_MemberTypePrimitive(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("FieldRefAccess", false, FieldRefTestType.Type, "System.Text.Json.Nodes.JsonObject", "System.Collections.Generic.List<string>", "_dictionary")]
        [DataRow("FieldRefAccess", false, FieldRefTestType.String, "System.Text.Json.Nodes.JsonObject", "System.Collections.Generic.List<string>", "_dictionary")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.Type, "System.Text.Json.Nodes.JsonObject", "System.Collections.Generic.List<string>", "_dictionary")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.String, "System.Text.Json.Nodes.JsonObject", "System.Collections.Generic.List<string>", "_dictionary")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.Type, "System.Numerics.Vector3", "System.Collections.Generic.List<string>", "X")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.String, "System.Numerics.Vector3", "System.Collections.Generic.List<string>", "X")]
        public async Task FieldRef_MemberTypeObject(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }
    }
}