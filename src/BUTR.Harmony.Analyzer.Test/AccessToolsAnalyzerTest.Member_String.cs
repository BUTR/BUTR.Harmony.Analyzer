using Microsoft.VisualStudio.TestTools.UnitTesting;

using System;
using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer.Test
{
    public partial class AccessToolsAnalyzerTest
    {
        [TestMethod]
        public async Task Field_String_Correct()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;

    class TestProgram
    {
        public static void TestMethod()
        {
            AccessTools2.Field(""System.Text.Json.JsonException:_message"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Field_String_CorrectMemberFromBase()
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
            AccessTools2.Field(""System.Text.Json.Nodes.JsonArray:_parent"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Field_String_IncorrectMember()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.Field(""System.Text.Json.JsonException:_message1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Field_String_IncorrectType()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.Field(""System.Text.Json.JsonException1:_message1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Field_String_Declared_IncorrectMemberFromBase()
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
            [||]AccessTools2.DeclaredField(""System.Text.Json.Nodes.JsonArray:_parent"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }


        [TestMethod]
        public async Task Property_String_Correct()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;

    class TestProgram
    {
        public static void TestMethod()
        {
            AccessTools2.Property(""System.Text.Json.JsonException:AppendPathInformation"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Property_String_CorrectMemberFromBase()
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
            AccessTools2.Property(""System.Text.Json.Nodes.JsonArray:Root"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Property_String_IncorrectMember()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.Property(""System.Text.Json.JsonException:AppendPathInformation1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Property_String_IncorrectType()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.Property(""System.Text.Json.JsonException1:AppendPathInformation1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Property_String_Declared_IncorrectMemberFromBase()
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
            [||]AccessTools2.DeclaredProperty(""System.Text.Json.Nodes.JsonArray:Root"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }


        [TestMethod]
        public async Task Method_String_Correct()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;

    class TestProgram
    {
        public static void TestMethod()
        {
            AccessTools2.Method(""System.Text.Json.JsonException:SetMessage"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Method_String_CorrectMemberFromBase()
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
            AccessTools2.Method(""System.Text.Json.Nodes.JsonArray:AssignParent"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Method_String_IncorrectMember()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.Method(""System.Text.Json.JsonException:SetMessage1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Method_String_IncorrectType()
        {
            var sourceCode = @"
namespace BUTR.Harmony.Analyzer.Test
{
    using HarmonyLib;
    using System;

    class TestProgram
    {
        public static void TestMethod()
        {
            [||]AccessTools2.Method(""System.Text.Json.JsonException1:SetMessage1"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }

        [TestMethod]
        public async Task Method_String_Declared_IncorrectMemberFromBase()
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
            [||]AccessTools2.DeclaredMethod(""System.Text.Json.Nodes.JsonArray:AssignParent"");
        }
    }
}
";
            await CreateProjectBuilder().WithSourceCode(HarmonyBase + Environment.NewLine + sourceCode).ValidateAsync();
        }
    }
}