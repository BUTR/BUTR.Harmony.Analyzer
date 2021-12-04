using BUTR.Harmony.Analyzer.Analyzers;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestHelper;

namespace BUTR.Harmony.Analyzer.Test
{
    [TestClass]
    public partial class AccessToolsAnalyzerTest
    {
        private static readonly string HarmonyBase = @"
namespace HarmonyLib
{
    using System;

    public class AccessTools2
    {
        public static object Field(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object Field(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredField(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredField(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;

        public static object Property(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object Property(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredProperty(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredProperty(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;

        public static object PropertyGetter(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object PropertyGetter(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredPropertyGetter(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredPropertyGetter(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;

        public static object PropertySetter(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object PropertySetter(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredPropertySetter(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredPropertySetter(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;

        public static object Method(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object Method(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredMethod(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredMethod(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;

        public static object StaticFieldRefAccess<T, F>(string fieldName)  => null;

        public static object StructFieldRefAccess<T, F>(string fieldName)  => null;
    }
}
";

        private static ProjectBuilder CreateProjectBuilder() => new ProjectBuilder()
            .WithAnalyzer<AccessToolsAnalyzer>()
            .AddSystemTextJson();
    }
}