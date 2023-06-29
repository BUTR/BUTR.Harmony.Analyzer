using BUTR.Harmony.Analyzer.Data;

using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class FieldRefAccessHelper
    {
        public static void FindAndReportForFieldRefAccess(GenericContext context, ITypeSymbol fieldType, string typeSemicolonFieldName, bool staticCheck)
        {
            var split = typeSemicolonFieldName.Split(':') ?? Array.Empty<string>();
            var typeName = split.Length > 0 ? split[0] : string.Empty;
            var fieldName = split.Length > 1 ? split[1] : string.Empty;;

            var type = FindAndReportForType(context, typeName);
            if (type is null)
            {
                return;
            }

            foreach (var diagnostic in DiagnosticsFieldRefAccess(context, type, fieldType, fieldName, staticCheck))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<Diagnostic> DiagnosticsFieldRefAccess(GenericContext context, ITypeSymbol objectType, ITypeSymbol fieldType, string fieldName, bool staticCheck)
        {
            if (context.Compilation.GetMetadataReference(objectType.ContainingAssembly) is PortableExecutableReference otr && File.Exists(otr.FilePath))
            {
                return CodeMetadataParser.FindMemberAndCheckType(context, otr.FilePath, objectType, fieldType, fieldName, staticCheck);
            }
            else // Fallback to roslyn based check. Mostly used when source code is available within the solution
            {
                return CodeRoslynParser.FindMemberAndCheckType(context, objectType, fieldType, fieldName, staticCheck);
            }
        }

        private static INamedTypeSymbol? FindAndReportForType(GenericContext context, string typeName)
        {
            var type = context.Compilation.GetAssemblies().Select(a => a.GetTypeByMetadataName(typeName)).FirstOrDefault(t => t is not null);
            if (type is null)
            {
                context.ReportDiagnostic(RuleIdentifiers.ReportType(context, typeName));
                return null;
            }

            return type;
        }
    }
}