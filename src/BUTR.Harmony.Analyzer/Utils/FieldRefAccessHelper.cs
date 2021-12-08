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
            var obectTypeRef = context.Compilation.GetMetadataReference(objectType.ContainingAssembly);
            //var fieldTypeRef = context.Compilation.GetMetadataReference(fieldType.ContainingAssembly);

            if (obectTypeRef is PortableExecutableReference otr && File.Exists(otr.FilePath)/* && fieldTypeRef is PortableExecutableReference ftr && File.Exists(ftr.FilePath)*/)
            {
                return CodeMetadataParser.FindMemberAndCheckType(context, otr.FilePath, objectType, fieldType, fieldName);
            }
            else // Fallback to roslyn based finding
            {
                return CodeRoslynParser.FindMemberAndCheckType(context, objectType, fieldType, fieldName);
            }
        }
    }
}