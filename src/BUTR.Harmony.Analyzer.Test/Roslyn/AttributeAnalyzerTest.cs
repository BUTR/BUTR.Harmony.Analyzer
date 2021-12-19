using BUTR.Harmony.Analyzer.Analyzers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TestHelper;

namespace BUTR.Harmony.Analyzer.Test.Roslyn
{
    [TestClass]
    public class AttributeAnalyzerTest : BaseTest
    {
        private static ProjectBuilder CreateProjectBuilder() => new ProjectBuilder()
            .WithAnalyzer<AttributeAnalyzer>()
            .AddSystemTextJson()
            .AddSystemMemory();

        public enum MethodType { Normal, Getter, Setter, Constructor, StaticConstructor }

        public enum ArgumentType { Normal, Ref, Out, Pointer }

        public enum MemberTestType
        {
            // ReSharper disable InconsistentNaming
            TypeOf_MethodName,
            TypeOf_MethodName_ParamTypes,
            TypeOf_MethodName_ParamTypes_ParamVariations,

            TypeOf_MethodType,
            TypeOf_MethodType_ParamTypes,
            TypeOf_MethodType_ParamTypes_ParamVariations,


            SeparateTypeOf_SeparateMethodName_MethodType,

            SeparateTypeOf_SeparateMethodName,
            SeparateTypeOf_SeparateMethodName_SeparateParamTypes,
            SeparateTypeOf_SeparateMethodName_SeparateParamTypes_ParamVariations,

            SeparateTypeOf_SeparateMethodType_ParamTypes,
            SeparateTypeOf_SeparateMethodType_ParamTypes_ParamVariations,

            SeparateTypeOf_SeparateMethodType_SeparateParamTypes,
            SeparateTypeOf_SeparateMethodType_SeparateParamTypes_ParamVariations,
            // ReSharper restore InconsistentNaming
        }

        private static string SourceCode(bool isCorrect, MemberTestType testType, string type, string member, MethodType methodType, IEnumerable<string> paramTypes, IEnumerable<ArgumentType> paramVariation) => @$"
{HarmonyBase}

namespace BUTR.Harmony.Analyzer.Test
{{
    using System;
    using HarmonyLib;

    class BaseTestClass
    {{
        private string _baseField;
        private bool _baseProperty {{ get; set; }}
        private int _baseMethod() {{ return 1; }}
        private int _baseMethod(string str) {{ return 1; }}
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
        private int _method(ref string str, int i) {{ return 1; }}
        private int _method(out string str, bool b) {{ str = """"; return 1; }}

        public TestClass() {{ }}
        public TestClass(string str) {{ }}
    }}

    {GetAttribute(isCorrect, testType, type, member, methodType, paramTypes, paramVariation)}
    class TestProgram {{ }}
}}
";

        private static string GetTypes(IEnumerable<string> paramTypes) => $"new Type[] {{ {string.Join(", ", paramTypes.Select(x => $"typeof({x})"))} }}";

        private static string GetVariations(IEnumerable<ArgumentType> paramVariation) => $"new ArgumentType[] {{ {string.Join(", ", paramVariation.Select(x => $"ArgumentType.{x}"))} }}";

        private static string GetAttribute(bool isCorrect, MemberTestType testType, string type, string member, MethodType methodType, IEnumerable<string> paramTypes, IEnumerable<ArgumentType> paramVariation) => testType switch
        {
            MemberTestType.TypeOf_MethodName => @$"
[{(isCorrect ? "" : "[||]")}HarmonyPatch(typeof({type}), ""{member}"")]",

            MemberTestType.TypeOf_MethodName_ParamTypes => @$"
[{(isCorrect ? "" : "[||]")}HarmonyPatch(typeof({type}), ""{member}"", {GetTypes(paramTypes)})]",
            MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations => @$"
[{(isCorrect ? "" : "[||]")}HarmonyPatch(typeof({type}), ""{member}"", {GetTypes(paramTypes)}, {GetVariations(paramVariation)})]",

            MemberTestType.TypeOf_MethodType => @$"
[{(isCorrect ? "" : "[||]")}HarmonyPatch(typeof({type}), MethodType.{methodType})]",
            MemberTestType.TypeOf_MethodType_ParamTypes => @$"
[{(isCorrect ? "" : "[||]")}HarmonyPatch(typeof({type}), MethodType.{methodType}, {GetTypes(paramTypes)})]",
            MemberTestType.TypeOf_MethodType_ParamTypes_ParamVariations => @$"
[{(isCorrect ? "" : "[||]")}HarmonyPatch(typeof({type}), MethodType.{methodType}, {GetTypes(paramTypes)}, {GetVariations(paramVariation)})]",

            MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType => @$"
[{(isCorrect ? "" : "[||]")}HarmonyPatch(typeof({type}))]
[HarmonyPatch(""{member}"", MethodType.{methodType})]",

            MemberTestType.SeparateTypeOf_SeparateMethodType_SeparateParamTypes => @$"
[{(isCorrect ? "" : "[||]")}HarmonyPatch(typeof({type}))]
[HarmonyPatch(""{member}"")]
[HarmonyPatch({GetTypes(paramTypes)})]",
            MemberTestType.SeparateTypeOf_SeparateMethodType_SeparateParamTypes_ParamVariations => @$"
[{(isCorrect ? "" : "[||]")}HarmonyPatch(typeof({type}))]
[HarmonyPatch(""{member}"")]
[HarmonyPatch({GetTypes(paramTypes)}, {GetVariations(paramVariation)})]",

            _ => ""
        };


        [DataTestMethod]

        [DataRow(true, MemberTestType.TypeOf_MethodType, "BUTR.Harmony.Analyzer.Test.TestClass", "", MethodType.Constructor, new string[] { }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodType_ParamTypes, "BUTR.Harmony.Analyzer.Test.TestClass", "", MethodType.Constructor, new[] { "string" }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodType_ParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", "", MethodType.Constructor, new[] { "string" }, new[] { ArgumentType.Normal })]

        [DataRow(true, MemberTestType.TypeOf_MethodName, "BUTR.Harmony.Analyzer.Test.TestClass", "_field", MethodType.Normal, new string[] { }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodName_ParamTypes, "BUTR.Harmony.Analyzer.Test.TestClass", "_field", MethodType.Normal, new[] { "string" }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", "_method", MethodType.Normal, new[] { "string", "int" }, new[] { ArgumentType.Ref, ArgumentType.Normal })]
        [DataRow(true, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", "_method", MethodType.Normal, new[] { "string", "bool" }, new[] { ArgumentType.Out, ArgumentType.Normal })]

        [DataRow(true, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", MethodType.Getter, new string[] { }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", MethodType.Getter, new string[] { }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "BUTR.Harmony.Analyzer.Test.TestClass", "_property", MethodType.Setter, new string[] { }, new ArgumentType[] { })]

        [DataRow(true, MemberTestType.SeparateTypeOf_SeparateMethodType_SeparateParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", "_method", MethodType.Normal, new[] { "string", "int" }, new[] { ArgumentType.Ref, ArgumentType.Normal })]
        public async Task Field_Default(bool isCorrect, MemberTestType testType, string type, string member, MethodType methodType, string[] paramTypes, ArgumentType[] paramVariations)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(isCorrect, testType, type, member, methodType, paramTypes, paramVariations)).ValidateAsync();
        }


        [DataTestMethod]

        [DataRow(false, MemberTestType.TypeOf_MethodType, "System.Convert", "", MethodType.Constructor, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.TypeOf_MethodType_ParamTypes, "BUTR.Harmony.Analyzer.Test.TestClass", "", MethodType.Constructor, new[] { "bool" }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.TypeOf_MethodType_ParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", "", MethodType.Constructor, new[] { "string" }, new[] { ArgumentType.Ref })]

        [DataRow(false, MemberTestType.TypeOf_MethodName, "BUTR.Harmony.Analyzer.Test.TestClass", "_field1", MethodType.Normal, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.TypeOf_MethodName_ParamTypes, "BUTR.Harmony.Analyzer.Test.TestClass", "_field1", MethodType.Normal, new[] { "string" }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", "_method", MethodType.Normal, new[] { "string", "int" }, new[] { ArgumentType.Ref, ArgumentType.Ref })]
        [DataRow(false, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "System.Collections.Generic.List<string>", "_method1", MethodType.Normal, new[] { "string", "int" }, new[] { ArgumentType.Ref, ArgumentType.Normal })]
        //[DataRow(false, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", "_method", MethodType.Normal, new[] { "ReadOnlySpan<byte>", "int" }, new[] { ArgumentType.Out, ArgumentType.Ref })]
        //[DataRow(false, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", "TryGetBytesFromBase64", MethodType.Normal, new[] { "byte[]" }, new[] { ArgumentType.Ref })]

        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertySet", MethodType.Getter, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet", MethodType.Setter, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertySet1", MethodType.Getter, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "BUTR.Harmony.Analyzer.Test.TestClass", "_propertyGet1", MethodType.Setter, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "BUTR.Harmony.Analyzer.Test.TestClass", "_field", MethodType.Getter, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "BUTR.Harmony.Analyzer.Test.TestClass", "_field", MethodType.Setter, new string[] { }, new ArgumentType[] { })]
        //[DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodType_SeparateParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", MethodType.Normal, "_method", new[] { "ReadOnlySpan<byte>", "int" }, new[] { ArgumentType.Out, ArgumentType.Ref })]
        public async Task Field_Member(bool isCorrect, MemberTestType testType, string type, string member, MethodType methodType, string[] paramTypes, ArgumentType[] paramVariations)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(isCorrect, testType, type, member, methodType, paramTypes, paramVariations)).ValidateAsync();
        }


        [DataTestMethod]

        [DataRow(true, MemberTestType.TypeOf_MethodName, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseProperty", MethodType.Normal, new string[] { }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodName_ParamTypes, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseMethod", MethodType.Normal, new[] { "string" }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseMethod", MethodType.Normal, new[] { "string" }, new ArgumentType[] { })]

        [DataRow(true, MemberTestType.SeparateTypeOf_SeparateMethodType_SeparateParamTypes_ParamVariations, "BUTR.Harmony.Analyzer.Test.TestClass", "_baseMethod", MethodType.Normal, new[] { "string" }, new ArgumentType[] { })]
        public async Task Field_MemberFromBase(bool isCorrect, MemberTestType testType, string type, string member, MethodType methodType, string[] paramTypes, ArgumentType[] paramVariations)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(isCorrect, testType, type, member, methodType, paramTypes, paramVariations)).ValidateAsync();
        }
    }
}