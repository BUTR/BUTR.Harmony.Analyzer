using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test
{
    public partial class AccessToolsAnalyzerTest
    {
        [TestMethod]
        public async Task FieldRef_Static_Correct()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;

    class TestProgram
    {
        public static void TestMethod()
        {
            AccessTools2.StaticFieldRefAccess<JsonException, string>(""_message"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task FieldRef_Static_IncorrectMember()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.StaticFieldRefAccess<JsonException, string>(""_message1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task FieldRef_Static_IncorrectMemberTypePrimitive()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.StaticFieldRefAccess<JsonException, bool>(""_message1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task FieldRef_Static_IncorrectMemberTypeObject()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Threading.Tasks;
    using System.Text.Json;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.StaticFieldRefAccess<JsonException, Task>(""_message1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }


        [TestMethod]
        public async Task FieldRef_Struct_Correct()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;

    class TestProgram
    {
        public static void TestMethod()
        {
            AccessTools2.StructFieldRefAccess<JsonException, string>(""_message"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task FieldRef_Struct_IncorrectMember()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.StructFieldRefAccess<JsonException, string>(""_message1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task FieldRef_Struct_IncorrectMemberTypePrimitive()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.StructFieldRefAccess<JsonException, bool>(""_message1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task FieldRef_Struct_IncorrectMemberTypeObject()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Threading.Tasks;
    using System.Text.Json;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.StructFieldRefAccess<JsonException, Task>(""_message1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }
    }
}