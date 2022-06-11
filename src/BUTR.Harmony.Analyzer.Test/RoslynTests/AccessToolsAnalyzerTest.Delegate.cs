using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test.Roslyn
{
    public partial class AccessToolsAnalyzerTest
    {
        private static string SourceCode(string method, bool isCorrect, MemberTestType testType, string type, string member, string @delegate) => @$"
{HarmonyBase}

namespace BUTR.Harmony.Analyzer.Test
{{
    public delegate TestClass CreateTestClass();
    public delegate TestClass CreateTestClassInt(int value);

    public delegate int CallInstanceMethod(object instance, string value);
    public delegate int CallMethod(string value);

    public delegate void SetInstancePropBool(object instance, bool value);
    public delegate bool GetInstancePropBool(object instance);
        
    public delegate void SetPropBool(bool value);
    public delegate bool GetPropBool();

    public class BaseTestClass
    {{
        protected bool _prop;
        private bool _baseProperty {{ get; set; }}
        private int _baseMethod() {{ return 1; }}
        private int _baseMethod(string str) {{ return 1; }}
        protected BaseTestClass(string str) {{ }}
    }}

    public class TestClass : BaseTestClass
    {{
        private bool _property {{ get; set; }}
        private bool _propertyGet => _prop;
        private bool _propertySet {{ set {{ _prop = value; }} }}
        private int _method() {{ return 1; }}
        private int _method(string str) {{ return 1; }}
        private TestClass(char c) : base(c.ToString()) {{ }}
    }}

    public class TestProgram
    {{
        public static void TestMethod()
        {{
            {(isCorrect ? "" : "[||]")}HarmonyLib.AccessTools.{method}<{@delegate}>({GetArg2(testType, type, member)});
        }}
    }}
}}
";

        private static string GetArg2(MemberTestType testType, string type, string member) => testType switch
        {
            MemberTestType.TypeOf => $"typeof({type}), \"{member}\"",
            MemberTestType.String => $"\"{type}:{member}\"",
            MemberTestType.TypeOfOnly => $"typeof({type}), new System.Type[] {{ {member} }}",
            MemberTestType.StringOnly => $"\"{type}\", new System.Type[] {{ {member} }}",
            _ => ""
        };

        ///

        [DataTestMethod]
        [DataRow("GetDeclaredDelegate", true,  MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_method", "CallInstanceMethod")]
        [DataRow("GetDelegate", true,  MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_method", "CallInstanceMethod")]
        
        [DataRow("GetDeclaredDelegate", true,  MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_method", "CallInstanceMethod")]
        [DataRow("GetDelegate", true,  MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_method", "CallInstanceMethod")]
        
        [DataRow("GetDeclaredDelegate", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingMethod", "SetInstancePropBool")]
        [DataRow("GetDelegate", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingMethod", "SetInstancePropBool")]
        
        [DataRow("GetDeclaredDelegate", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingMethod", "SetInstancePropBool")]
        [DataRow("GetDelegate", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingMethod", "SetInstancePropBool")]
        public async Task Delegate_Default(string method, bool isCorrect, MemberTestType testType, string type, string member, string @delegate)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member, @delegate)).ValidateAsync();
        }
        
        [DataTestMethod]
        [DataRow("GetDeclaredPropertyGetterDelegate", true,  MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", "GetInstancePropBool")]
        [DataRow("GetPropertyGetterDelegate", true,  MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", "GetInstancePropBool")]
        [DataRow("GetDeclaredPropertyGetterDelegate", true,  MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", "GetInstancePropBool")]
        [DataRow("GetPropertyGetterDelegate", true,  MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", "GetInstancePropBool")]
        
        [DataRow("GetDeclaredPropertyGetterDelegate", true,  MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet", "GetInstancePropBool")]
        [DataRow("GetPropertyGetterDelegate", true,  MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet", "GetInstancePropBool")]
        [DataRow("GetDeclaredPropertyGetterDelegate", true,  MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet", "GetInstancePropBool")]
        [DataRow("GetPropertyGetterDelegate", true,  MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet", "GetInstancePropBool")]
        
        [DataRow("GetDeclaredPropertyGetterDelegate", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingProperty", "GetInstancePropBool")]
        [DataRow("GetPropertyGetterDelegate", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingProperty", "GetInstancePropBool")]
        [DataRow("GetDeclaredPropertyGetterDelegate", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingProperty", "GetInstancePropBool")]
        [DataRow("GetPropertyGetterDelegate", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingProperty", "GetInstancePropBool")]
        
        [DataRow("GetDeclaredPropertySetterDelegate", true,  MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", "SetInstancePropBool")]
        [DataRow("GetPropertySetterDelegate", true,  MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", "SetInstancePropBool")]
        [DataRow("GetDeclaredPropertySetterDelegate", true,  MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", "SetInstancePropBool")]
        [DataRow("GetPropertySetterDelegate", true,  MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", "SetInstancePropBool")]
        
        [DataRow("GetDeclaredPropertySetterDelegate", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet", "SetInstancePropBool")]
        [DataRow("GetPropertySetterDelegate", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet", "SetInstancePropBool")]
        [DataRow("GetDeclaredPropertySetterDelegate", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet", "SetInstancePropBool")]
        [DataRow("GetPropertySetterDelegate", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet", "SetInstancePropBool")]
        
        [DataRow("GetDeclaredPropertySetterDelegate", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingProperty", "SetInstancePropBool")]
        [DataRow("GetPropertySetterDelegate", false, MemberTestType.TypeOf, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingProperty", "SetInstancePropBool")]
        [DataRow("GetDeclaredPropertySetterDelegate", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingProperty", "SetInstancePropBool")]
        [DataRow("GetPropertySetterDelegate", false, MemberTestType.String, "BUTR.Harmony.Analyzer.Test.TestClass", "NonExistingProperty", "SetInstancePropBool")]
        public async Task Delegate_Property(string method, bool isCorrect, MemberTestType testType, string type, string member, string @delegate)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member, @delegate)).ValidateAsync();
        }
        
        [DataTestMethod]
        [DataRow("GetDeclaredConstructorDelegate", false, MemberTestType.TypeOfOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "", "CreateTestClass")]
        [DataRow("GetConstructorDelegate", true, MemberTestType.TypeOfOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "", "CreateTestClass")]
        [DataRow("GetDeclaredConstructorDelegate", false, MemberTestType.StringOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "", "CreateTestClass")]
        [DataRow("GetConstructorDelegate", true, MemberTestType.StringOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "", "CreateTestClass")]
        
        [DataRow("GetDeclaredConstructorDelegate", false, MemberTestType.TypeOfOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "typeof(int)", "CreateTestClassInt")]
        [DataRow("GetConstructorDelegate", false, MemberTestType.TypeOfOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "typeof(int)", "CreateTestClassInt")]
        [DataRow("GetDeclaredConstructorDelegate", false, MemberTestType.StringOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "typeof(int)", "CreateTestClassInt")]
        [DataRow("GetConstructorDelegate", false, MemberTestType.StringOnly, "BUTR.Harmony.Analyzer.Test.TestClass", "typeof(int)", "CreateTestClassInt")]
        public async Task Delegate_Constructor(string method, bool isCorrect, MemberTestType testType, string type, string member, string @delegate)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(method, isCorrect, testType, type, member, @delegate)).ValidateAsync();
        }
    }
}