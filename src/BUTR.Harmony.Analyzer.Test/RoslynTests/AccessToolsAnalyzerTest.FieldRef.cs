using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test.Roslyn
{
    public partial class AccessToolsAnalyzerTest
    {
        public enum FieldRefTestType { Type, String }

        private static string SourceCode(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName) => @$"
{HarmonyBase}

namespace BUTR.Harmony.Analyzer.Test
{{
    class TestClass
    {{
        private string _field;
        private static string _staticField;
    }}

    struct TestStruct
    {{
        private string _field;
    }}


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

        [DataTestMethod]
        [DataRow("FieldRefAccess", true, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_field")]
        [DataRow("FieldRefAccess", true, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_field")]
        [DataRow("StaticFieldRefAccess", true, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_staticField")]
        [DataRow("StaticFieldRefAccess", true, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_staticField")]
        [DataRow("StructFieldRefAccess", true, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestStruct", "string", "_field")]
        [DataRow("StructFieldRefAccess", true, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestStruct", "string", "_field")]
        public async Task FieldRef_Default(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("FieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_field1")]
        [DataRow("FieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_field1")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_staticField1")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_staticField1")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestStruct", "string", "_field1")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestStruct", "string", "_field1")]
        public async Task FieldRef_Member(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("FieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "bool", "_field")]
        [DataRow("FieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "bool", "_field")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "bool", "_staticField")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "bool", "_staticField")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestStruct", "bool", "_field")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestStruct", "bool", "_field")]
        public async Task FieldRef_MemberTypePrimitive(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("FieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "System.Collections.Generic.List<string>", "_field")]
        [DataRow("FieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "System.Collections.Generic.List<string>", "_field")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "System.Collections.Generic.List<string>", "_staticField")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "System.Collections.Generic.List<string>", "_staticField")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestStruct", "System.Collections.Generic.List<string>", "_field")]
        [DataRow("StructFieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestStruct", "System.Collections.Generic.List<string>", "_field")]
        public async Task FieldRef_MemberTypeObject(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }
        
        [DataTestMethod]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_field")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_field")]
        public async Task FieldRef_MemberNotStatic(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }
        
        [DataTestMethod]
        [DataRow("FieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_staticField")]
        [DataRow("FieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_staticField")]
        public async Task FieldRef_MemberNotInstance(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }
        
        [DataTestMethod]
        [DataRow("FieldRefAccess", true, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_field")]
        [DataRow("FieldRefAccess", true, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_field")]
        [DataRow("StaticFieldRefAccess", true, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_staticField")]
        [DataRow("StaticFieldRefAccess", true, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "string", "_staticField")]
        
        [DataRow("FieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "int", "_field")]
        [DataRow("FieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "int", "_field")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "int", "_staticField")]
        [DataRow("StaticFieldRefAccess", false, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "int", "_staticField")]
        
        [DataRow("FieldRefAccess", true, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "object", "_field")]
        [DataRow("FieldRefAccess", true, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "object", "_field")]
        [DataRow("StaticFieldRefAccess", true, FieldRefTestType.Type, "BUTR.Harmony.Analyzer.Test.TestClass", "object", "_staticField")]
        [DataRow("StaticFieldRefAccess", true, FieldRefTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "object", "_staticField")]
        public async Task FieldRef_MemberWrongType(string method, bool isCorrect, FieldRefTestType testType, string objectType, string fieldType, string fieldName)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, objectType, fieldType, fieldName)).ValidateAsync();
        }
    }
}