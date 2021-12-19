using BUTR.Harmony.Analyzer.Analyzers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TestHelper;

namespace BUTR.Harmony.Analyzer.Test.Metadata
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

        [DataRow(true, MemberTestType.TypeOf_MethodType, "System.Text.Json.JsonException", "", MethodType.Constructor, new string[] { }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodType_ParamTypes, "System.Text.Json.JsonException", "", MethodType.Constructor, new[] { "string" }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodType_ParamTypes_ParamVariations, "System.Text.Json.JsonException", "", MethodType.Constructor, new[] { "string" }, new[] { ArgumentType.Normal })]

        [DataRow(true, MemberTestType.TypeOf_MethodName, "System.Text.Json.JsonException", "_message", MethodType.Normal, new string[] { }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodName_ParamTypes, "System.Text.Json.JsonException", "_message", MethodType.Normal, new[] { "string" }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "System.Text.Json.Utf8JsonReader", "ConsumeNegativeSign", MethodType.Normal, new[] { "ReadOnlySpan<byte>", "int" }, new[] { ArgumentType.Ref, ArgumentType.Ref })]
        [DataRow(true, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "System.Text.Json.Utf8JsonReader", "TryGetBytesFromBase64", MethodType.Normal, new[] { "byte[]" }, new[] { ArgumentType.Out })]

        [DataRow(true, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "System.Text.Json.JsonException", "Message", MethodType.Getter, new string[] { }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "System.Text.Json.JsonException", "AppendPathInformation", MethodType.Getter, new string[] { }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "System.Text.Json.JsonException", "AppendPathInformation", MethodType.Setter, new string[] { }, new ArgumentType[] { })]

        [DataRow(true, MemberTestType.SeparateTypeOf_SeparateMethodType_SeparateParamTypes_ParamVariations, "System.Text.Json.Utf8JsonReader", "ConsumeNegativeSign", MethodType.Normal, new[] { "ReadOnlySpan<byte>", "int" }, new[] { ArgumentType.Ref, ArgumentType.Ref })]
        public async Task Field_Default(bool isCorrect, MemberTestType testType, string type, string member, MethodType methodType, string[] paramTypes, ArgumentType[] paramVariations)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(isCorrect, testType, type, member, methodType, paramTypes, paramVariations)).ValidateAsync();
        }


        [DataTestMethod]

        [DataRow(false, MemberTestType.TypeOf_MethodType, "System.Convert", "", MethodType.Constructor, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.TypeOf_MethodType_ParamTypes, "System.Text.Json.JsonException", "", MethodType.Constructor, new[] { "bool" }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.TypeOf_MethodType_ParamTypes_ParamVariations, "System.Text.Json.JsonException", "", MethodType.Constructor, new[] { "string" }, new[] { ArgumentType.Ref })]

        [DataRow(false, MemberTestType.TypeOf_MethodName, "System.Text.Json.JsonException", "_message1", MethodType.Normal, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.TypeOf_MethodName_ParamTypes, "System.Text.Json.JsonException", "_message1", MethodType.Normal, new[] { "string" }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "System.Text.Json.Utf8JsonReader", "ConsumeNegativeSign", MethodType.Normal, new[] { "string", "int" }, new[] { ArgumentType.Ref, ArgumentType.Ref })]
        [DataRow(false, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "System.Collections.Generic.List<string>", "ConsumeNegativeSign1", MethodType.Normal, new[] { "ReadOnlySpan<byte>", "int" }, new[] { ArgumentType.Ref, ArgumentType.Ref })]
        //[DataRow(false, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "System.Text.Json.Utf8JsonReader", "ConsumeNegativeSign", MethodType.Normal, new[] { "ReadOnlySpan<byte>", "int" }, new[] { ArgumentType.Out, ArgumentType.Ref })]
        //[DataRow(false, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "System.Text.Json.Utf8JsonReader", "TryGetBytesFromBase64", MethodType.Normal, new[] { "byte[]" }, new[] { ArgumentType.Ref })]

        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "System.Text.Json.JsonException", "Message", MethodType.Setter, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "System.Text.Json.JsonException", "AppendPathInformation1", MethodType.Getter, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "System.Text.Json.JsonException", "AppendPathInformation1", MethodType.Setter, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "System.Text.Json.JsonException", "_message", MethodType.Getter, new string[] { }, new ArgumentType[] { })]
        [DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodName_MethodType, "System.Text.Json.JsonException", "_message", MethodType.Setter, new string[] { }, new ArgumentType[] { })]
        //[DataRow(false, MemberTestType.SeparateTypeOf_SeparateMethodType_SeparateParamTypes_ParamVariations, "System.Text.Json.Utf8JsonReader", MethodType.Normal, "ConsumeNegativeSign", new[] { "ReadOnlySpan<byte>", "int" }, new[] { ArgumentType.Out, ArgumentType.Ref })]
        public async Task Field_Member(bool isCorrect, MemberTestType testType, string type, string member, MethodType methodType, string[] paramTypes, ArgumentType[] paramVariations)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(isCorrect, testType, type, member, methodType, paramTypes, paramVariations)).ValidateAsync();
        }


        [DataTestMethod]

        [DataRow(true, MemberTestType.TypeOf_MethodName, "System.Text.Json.Nodes.JsonArray", "_parent", MethodType.Normal, new string[] { }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodName_ParamTypes, "System.Text.Json.Nodes.JsonArray", "_parent", MethodType.Normal, new[] { "string" }, new ArgumentType[] { })]
        [DataRow(true, MemberTestType.TypeOf_MethodName_ParamTypes_ParamVariations, "System.Text.Json.Nodes.JsonArray", "_parent", MethodType.Normal, new[] { "string" }, new ArgumentType[] { })]

        [DataRow(true, MemberTestType.SeparateTypeOf_SeparateMethodType_SeparateParamTypes_ParamVariations, "System.Text.Json.Nodes.JsonArray", "_parent", MethodType.Normal, new[] { "string" }, new ArgumentType[] { })]
        public async Task Field_MemberFromBase(bool isCorrect, MemberTestType testType, string type, string member, MethodType methodType, string[] paramTypes, ArgumentType[] paramVariations)
        {
            await CreateProjectBuilder().WithSourceCode(SourceCode(isCorrect, testType, type, member, methodType, paramTypes, paramVariations)).ValidateAsync();
        }
    }
}