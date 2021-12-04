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

    public class Harmony
    {
        public void Patch(object obj1, object obj2) { }
    }
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
        public static object Method(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object Method(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredMethod(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object DeclaredMethod(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;
        public static object StaticFieldRefAccess<T, F>(string fieldName)  => null;
        public static object StructFieldRefAccess<T, F>(string fieldName)  => null;
    }
    public class HarmonyMethod
    {
        public HarmonyMethod(object obj1, object obj2) { }
    }
}
";

        private static ProjectBuilder CreateProjectBuilder() => new ProjectBuilder()
            .WithAnalyzer<AccessToolsAnalyzer>()
            .AddSystemTextJson();

        /*
        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            var sourceCode = @"
namespace TaleWorlds.TwoDimension
{
    public struct EmprtStruct { }

    public class EditableText
    {

        private EmprtStruct _proprt;
        private void GetCursorPosition() { }
    }
}

namespace Bannerlord.HarmonyAnalyzer.Test
{
    using System;
    using System.Collections.Generic;
    using TaleWorlds.Core;
    using TaleWorlds.TwoDimension;

    class Test<TSelectorItemVM> where TSelectorItemVM : EditableText
    {
        public class Harmony
        {
            public void Patch(object obj1, object obj2) { }
        }
        public class AccessTools2
        {
            public static object Method(Type type, string name, Type[]? parameters = null, Type[]? generics = null) => null;
            public static object Method(string typeColonMethodname, Type[]? parameters = null, Type[]? generics = null) => null;
            public static object StaticFieldRefAccess<T, F>(string fieldName)  => null;
            public static object StructFieldRefAccess<T, F>(string fieldName)  => null;
        }
        public class HarmonyMethod
        {
            public HarmonyMethod(object obj1, object obj2) { }
        }

        public class EditableTextPatch { }

        public static void Patch(Harmony harmony)
        {
            var t = AccessTools2.StaticFieldRefAccess<BannerManager, string>(""_bannerIconGroups""); // EmprtStruct
            //harmony.Patch(
            //    //AccessTools2.Method(""Bannerlord.HarmonyAnalyzer.Test.TaleWorlds.TwoDimension.EditableText:GetCursorPosition""),
            //    [||]AccessTools2.Method(typeof(TSelectorItemVM), ""LoadXmlFile""),
            //    new HarmonyMethod(typeof(EditableTextPatch), ""GetCursorPosition""));
        }
    }
}
";

            await new ProjectBuilder()
            {
                References =
                    {
                        //AssemblyMetadata.CreateFromFile("D:\\SteamLibrary\\steamapps\\common\\Mount & Blade II Bannerlord\\bin\\Win64_Shipping_Client\\TaleWorlds.Core.dll").GetReference()
                        MetadataReference.CreateFromImage(
                            await File.ReadAllBytesAsync("D:\\SteamLibrary\\steamapps\\common\\Mount & Blade II Bannerlord\\bin\\Win64_Shipping_Client\\TaleWorlds.Core.dll"),
                            filePath: "D:\\SteamLibrary\\steamapps\\common\\Mount & Blade II Bannerlord\\bin\\Win64_Shipping_Client\\TaleWorlds.Core.dll"
                            )
                    }
            }
                .WithAnalyzer<AccessToolsAnalyzer>()
                .WithSourceCode(sourceCode)
                .ValidateAsync();
        }
        */
    }
}
