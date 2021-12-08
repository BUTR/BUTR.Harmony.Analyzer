using BUTR.Harmony.Analyzer.Analyzers;
using BUTR.Harmony.Analyzer.Data;

using Microsoft.CodeAnalysis;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class MemberHelper
    {
        public static void FindAndReportForAnyTypeMember(GenericContext context, ITypeSymbol typeSymbol, string memberName, MethodType methodType, ImmutableArray<ITypeSymbol>? paramTypes, ImmutableArray<ArgumentType>? paramVariations)
        {
            static MemberFlags GetForProperty(MethodType methodType) => methodType switch
            {
                MethodType.Getter => MemberFlags.Getter,
                MethodType.Setter => MemberFlags.Setter,
                _ => MemberFlags.None
            };
            static MemberFlags GetForMethod(MethodType methodType) => methodType switch
            {
                MethodType.Constructor => MemberFlags.Constructor,
                MethodType.StaticConstructor => MemberFlags.StaticConstructor,
                _ => MemberFlags.None
            };

            switch (methodType)
            {
                case MethodType.Normal:
                {
                    var diagnostics = new Dictionary<MemberFlags, ImmutableArray<Diagnostic>>
                    {
                        {MemberFlags.Field, DiagnosticsForMember(context, typeSymbol, MemberFlags.Field, memberName, paramTypes, paramVariations).ToImmutableArray()},
                        {MemberFlags.Property, DiagnosticsForMember(context, typeSymbol, MemberFlags.Property, memberName, paramTypes, paramVariations).ToImmutableArray()},
                        {MemberFlags.Method, DiagnosticsForMember(context, typeSymbol, MemberFlags.Method, memberName, paramTypes, paramVariations).ToImmutableArray()}
                    };
                    // If every type has an error, display all of them
                    // If at least one type held the value, we are correct
                    if (diagnostics.All(kv => !kv.Value.IsEmpty))
                    {
                        var typeName = NameFormatter.ReflectionName(typeSymbol);
                        //var typeName = typeSymbol.ToDisplayString(MetadataHelper.Style);
                        context.ReportDiagnostic(RuleIdentifiers.ReportMember(context, typeName, memberName));
                    }

                    break;
                }
                case MethodType.Getter:
                case MethodType.Setter:
                {
                    foreach (var diagnostic in DiagnosticsForMember(context, typeSymbol, MemberFlags.Property | GetForProperty(methodType), memberName, paramTypes, paramVariations))
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                    break;
                }
                case MethodType.Constructor:
                case MethodType.StaticConstructor:
                {
                    foreach (var diagnostic in DiagnosticsForMember(context, typeSymbol, MemberFlags.Method | GetForMethod(methodType), memberName, paramTypes, paramVariations))
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                    break;
                }
            }
        }

        public static void FindAndReportForMembers(GenericContext context, ImmutableArray<ITypeSymbol> typeSymbols, MemberFlags memberFlags, string memberName)
        {
            var diagnostics = new Dictionary<ITypeSymbol, ImmutableArray<Diagnostic>>();
            foreach (var typeSymbol in typeSymbols)
            {
                diagnostics.Add(typeSymbol, DiagnosticsForMember(context, typeSymbol, memberFlags, memberName, null, null).ToImmutableArray());
            }

            // If every type has an error, display all of them
            // If at least one type held the value, we are correct
            if (diagnostics.All(kv => !kv.Value.IsEmpty))
            {
                foreach (var diagnostic in diagnostics.Values.SelectMany(x => x))
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        public static void FindAndReportForMember(GenericContext context, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName)
        {
            foreach (var diagnostic in DiagnosticsForMember(context, typeSymbol, memberFlags, memberName, null, null))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        public static void FindAndReportForMember(GenericContext context, string typeSemicolonMember, MemberFlags memberFlags)
        {
            var split = typeSemicolonMember.Split(':');
            var typeName = split[0];
            var memberName = split[1];

            var type = RoslynHelper.GetAssemblies(context).Select(a => a.GetTypeByMetadataName(typeName)).FirstOrDefault(t => t is not null);
            if (type is null)
            {
                context.ReportDiagnostic(RuleIdentifiers.ReportType(context, typeName));
                return;
            }

            foreach (var diagnostic in DiagnosticsForMember(context, type, memberFlags, memberName, null, null))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }


        private static IEnumerable<Diagnostic> DiagnosticsForMember(GenericContext context, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName, ImmutableArray<ITypeSymbol>? paramTypes, ImmutableArray<ArgumentType>? paramVariations)
        {
            var typeSymbolRef = context.Compilation.GetMetadataReference(typeSymbol.ContainingAssembly);
            if (typeSymbolRef is PortableExecutableReference @ref && File.Exists(@ref.FilePath))
            {
                return CodeMetadataParser.FindMember(context, @ref.FilePath, typeSymbol, memberFlags, memberName, paramTypes, paramVariations);
            }
            else // Fallback to roslyn based finding
            {
                return CodeRoslynParser.FindMember(context, typeSymbol, memberFlags, memberName, paramTypes, paramVariations);
            }
        }
    }
}