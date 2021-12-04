using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test
{
    public partial class AccessToolsAnalyzerTest
    {
        [TestMethod]
        public async Task Field_TypeOf_Correct()
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
            AccessTools2.Field(typeof(JsonException), ""_message"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Field_TypeOf_CorrectMemberFromBase()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    class TestProgram
    {
        public static void TestMethod()
        {
            AccessTools2.Field(typeof(JsonArray), ""_parent"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Field_TypeOf_IncorrectMember()
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
            [||]AccessTools2.Field(typeof(JsonException), ""_message1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Field_TypeOf_Declared_Correct()
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
            AccessTools2.DeclaredField(typeof(JsonException), ""_message"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Field_TypeOf_Declared_IncorrectMember()
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
            [||]AccessTools2.DeclaredField(typeof(JsonException), ""_message1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Field_TypeOf_Declared_IncorrectMemberFromBase()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.DeclaredField(typeof(JsonArray), ""_parent"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }


        [TestMethod]
        public async Task Property_TypeOf_Correct()
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
            AccessTools2.Property(typeof(JsonException), ""AppendPathInformation"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Property_TypeOf_CorrectMemberFromBase()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    class TestProgram
    {
        public static void TestMethod()
        {
            AccessTools2.Property(typeof(JsonArray), ""Root"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Property_TypeOf_IncorrectMember()
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
            [||]AccessTools2.Property(typeof(JsonException), ""AppendPathInformation1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Property_TypeOf_Declared_Correct()
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
            AccessTools2.DeclaredProperty(typeof(JsonException), ""AppendPathInformation"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Property_TypeOf_Declared_IncorrectMember()
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
            [||]AccessTools2.DeclaredProperty(typeof(JsonException), ""AppendPathInformation1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Property_TypeOf_Declared_IncorrectMemberFromBase()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.DeclaredProperty(typeof(JsonArray), ""Root"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }


        [TestMethod]
        public async Task Method_TypeOf_Correct()
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
            AccessTools2.Method(typeof(JsonException), ""SetMessage"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Method_TypeOf_CorrectMemberFromBase()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    class TestProgram
    {
        public static void TestMethod()
        {
            AccessTools2.Method(typeof(JsonArray), ""AssignParent"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Method_TypeOf_IncorrectMember()
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
            [||]AccessTools2.Method(typeof(JsonException), ""SetMessage1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Method_TypeOf_Declared_Correct()
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
            AccessTools2.DeclaredMethod(typeof(JsonException), ""SetMessage"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Method_TypeOf_Declared_IncorrectMember()
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
            [||]AccessTools2.DeclaredMethod(typeof(JsonException), ""SetMessage1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Method_TypeOf_Declared_IncorrectMemberFromBase()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.DeclaredMethod(typeof(JsonArray), ""AssignParent"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }
    }
}