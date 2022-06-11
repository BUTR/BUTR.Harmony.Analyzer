using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test.Roslyn
{
    public partial class AccessToolsAnalyzerTest
    {
        public enum MemberTestType { TypeOf, String, TypeOfOnly, StringOnly }

        private static string SourceCode(string method, bool isCorrect, MemberTestType testType, string type, string member) => @$"
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
        [DataRow("Field", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_field")]
        [DataRow("Field", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_field")]
        [DataRow("DeclaredField", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_field")]
        [DataRow("DeclaredField", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_field")]
        public async Task Field_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Field", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_field1")]
        [DataRow("Field", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_field1")]
        [DataRow("DeclaredField", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_field1")]
        [DataRow("DeclaredField", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_field1")]
        public async Task Field_Member(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Field", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseField")]
        [DataRow("Field", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseField")]
        [DataRow("DeclaredField", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseField")]
        [DataRow("DeclaredField", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseField")]
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
        [DataRow("Property", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("Property", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("DeclaredProperty", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("DeclaredProperty", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        public async Task Property_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Property", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property1")]
        [DataRow("Property", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property1")]
        [DataRow("DeclaredProperty", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property1")]
        [DataRow("DeclaredProperty", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property1")]
        public async Task Property_Member(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Property", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseProperty")]
        [DataRow("Property", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseProperty")]
        [DataRow("DeclaredProperty", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseProperty")]
        [DataRow("DeclaredProperty", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseProperty")]
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
        [DataRow("PropertyGetter", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("PropertyGetter", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("DeclaredPropertyGetter", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("DeclaredPropertyGetter", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        public async Task PropertyGetter_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("PropertyGetter", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertySet")]
        [DataRow("PropertyGetter", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertySet")]
        [DataRow("DeclaredPropertyGetter", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertySet")]
        [DataRow("DeclaredPropertyGetter", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertySet")]
        public async Task PropertyGetter_Missing(string method, bool isCorrect, MemberTestType testType, string type, string member)
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
        [DataRow("PropertySetter", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("PropertySetter", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("DeclaredPropertySetter", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        [DataRow("DeclaredPropertySetter", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property")]
        public async Task PropertySetter_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("PropertySetter", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet")]
        [DataRow("PropertySetter", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet")]
        [DataRow("DeclaredPropertySetter", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet")]
        [DataRow("DeclaredPropertySetter", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet")]
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
        [DataRow("Method", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_method")]
        [DataRow("Method", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_method")]
        [DataRow("DeclaredMethod", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_method")]
        [DataRow("DeclaredMethod", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_method")]
        public async Task Method_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Method", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_method1")]
        [DataRow("Method", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_method1")]
        [DataRow("DeclaredMethod", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_method1")]
        [DataRow("DeclaredMethod", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_method1")]
        public async Task Method_Member(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Method", true, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseMethod")]
        [DataRow("Method", true, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseMethod")]
        [DataRow("DeclaredMethod", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseMethod")]
        [DataRow("DeclaredMethod", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseMethod")]
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
        [DataRow("Constructor", true, MemberTestType.TypeOfOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "typeof(char)")]
        [DataRow("DeclaredConstructor", true, MemberTestType.TypeOfOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "typeof(char)")]
        [DataRow("DeclaredConstructor", true, MemberTestType.StringOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "typeof(char)")]
        [DataRow("DeclaredConstructor", false, MemberTestType.TypeOfOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "")]
        [DataRow("DeclaredConstructor", false, MemberTestType.StringOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "")]
        public async Task Constructor_Default(string method, bool isCorrect, MemberTestType testType, string type, string member)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member)).ValidateAsync();
        }

        [DataTestMethod]
        [DataRow("Constructor", true, MemberTestType.TypeOfOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "typeof(string)")]
        [DataRow("DeclaredConstructor", false, MemberTestType.TypeOfOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "typeof(string)")]
        [DataRow("DeclaredConstructor", false, MemberTestType.StringOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "typeof(string)")]
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