using BUTR.Harmony.Analyzer.Data;

using Microsoft.CodeAnalysis;

using System.Collections.Generic;
using System.IO;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class FieldRefAccessHelper
    {
        public static void FindAndReportForFieldRefAccess(GenericContext context, ITypeSymbol objectType, ITypeSymbol fieldType, string fieldName)
        {
            foreach (var diagnostic in DiagnosticsFieldRefAccess(context, objectType, fieldType, fieldName))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<Diagnostic> DiagnosticsFieldRefAccess(GenericContext context, ITypeSymbol objectType, ITypeSymbol fieldType, string fieldName)
        {
            if (context.Compilation.GetMetadataReference(objectType.ContainingAssembly) is PortableExecutableReference otr && File.Exists(otr.FilePath))
            {
                return CodeMetadataParser.FindMemberAndCheckType(context, otr.FilePath, objectType, fieldType, fieldName);
            }
            else // Fallback to roslyn based check. Mostly used when source code is available within the solution
            {
                return CodeRoslynParser.FindMemberAndCheckType(context, objectType, fieldType, fieldName);
            }
        }
    }
}