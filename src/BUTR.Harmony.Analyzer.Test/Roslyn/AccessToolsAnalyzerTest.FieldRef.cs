using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test.Roslyn
{
    public partial class AccessToolsAnalyzerTest
    {
        private static string SourceCode(string method, bool isCorrect, string objectType, string fieldType, string fieldName) => @$"
{HarmonyBase}

namespace BUTR.Harmony.Analyzer.Test
{{
    class TestClass
    {{
        private string _field;
    }}

    struct TestStruct
    {{
        private string _field;
    }}


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
        [DataRow("StaticFieldRefAccess", true, "TestClass", "string", "_field")]
        [DataRow("StructFieldRefAccess", true, "TestStruct", "string", "_field")]
        public async Task FieldRef_Default(string method, bool isCorrect, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("StaticFieldRefAccess", false, "TestClass", "string", "_field1")]
        [DataRow("StructFieldRefAccess", false, "TestStruct", "string", "_field1")]

        public async Task FieldRef_Member(string method, bool isCorrect, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("StaticFieldRefAccess", false, "TestClass", "bool", "_field")]
        [DataRow("StructFieldRefAccess", false, "TestStruct", "bool", "_field")]
        public async Task FieldRef_MemberTypePrimitive(string method, bool isCorrect, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("StaticFieldRefAccess", false, "TestClass", "System.Collections.Generic.List<string>", "_field")]
        [DataRow("StructFieldRefAccess", false, "TestStruct", "System.Collections.Generic.List<string>", "_field")]
        public async Task FieldRef_MemberTypeObject(string method, bool isCorrect, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, objectType, fieldType, fieldName)).ValidateAsync();
        }
    }
}