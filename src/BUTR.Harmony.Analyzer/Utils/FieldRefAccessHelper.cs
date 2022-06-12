using BUTR.Harmony.Analyzer.Data;

using Microsoft.CodeAnalysis;

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class FieldRefAccessHelper
    {
        public static void FindAndReportForFieldRefAccess(GenericContext context, ITypeSymbol fieldType, string typeSemicolonFieldname, bool staticCheck)
        {
            var split = typeSemicolonFieldname.Split(':');
            var typeName = split[0];
            var fieldname = split[1];
            
            var type = FindAndReportForType(context, typeName);
            if (type is null)
            {
                return;
            }

            foreach (var diagnostic in DiagnosticsFieldRefAccess(context, type, fieldType, fieldname, staticCheck))
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