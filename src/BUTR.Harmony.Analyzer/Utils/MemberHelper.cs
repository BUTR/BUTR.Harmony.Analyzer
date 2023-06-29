using BUTR.Harmony.Analyzer.Data;

using Microsoft.CodeAnalysis;

using System;
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
                        {MemberFlags.Field, DiagnosticsForMember(context, typeSymbol, MemberFlags.Field | MemberFlags.Declared, memberName, paramTypes, paramVariations).ToImmutableArray()},
                        {MemberFlags.Property, DiagnosticsForMember(context, typeSymbol, MemberFlags.Property | MemberFlags.Declared, memberName, paramTypes, paramVariations).ToImmutableArray()},
                        {MemberFlags.Method, DiagnosticsForMember(context, typeSymbol, MemberFlags.Method | MemberFlags.Declared, memberName, paramTypes, paramVariations).ToImmutableArray()}
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
                    foreach (var diagnostic in DiagnosticsForMember(context, typeSymbol, MemberFlags.Property | GetForProperty(methodType) | MemberFlags.Declared, memberName, paramTypes, paramVariations))
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                    break;
                }
                case MethodType.Constructor:
                case MethodType.StaticConstructor:
                {
                    foreach (var diagnostic in DiagnosticsForMember(context, typeSymbol, MemberFlags.Method | GetForMethod(methodType) | MemberFlags.Declared, memberName, paramTypes, paramVariations))
                    {
                        context.ReportDiagnostic(diagnostic);
                    }
                    break;
                }
            }
        }

        public static void FindAndReportForMembers(GenericContext context, ImmutableArray<string> typeSemicolonMembers, MemberFlags memberFlags)
        {
            var diagnostics = new Dictionary<ITypeSymbol, ImmutableArray<Diagnostic>>();
            foreach (var typeSemicolonMember in typeSemicolonMembers)
            {
                var split = typeSemicolonMember.Split(':') ?? Array.Empty<string>();
                var typeName = split.Length > 0 ? split[0] : string.Empty;
                var memberName = split.Length > 1 ? split[1] : string.Empty;;

                var type = FindAndReportForType(context, typeName);
                if (type is null)
                {
                    return;
                }

                diagnostics.Add(type, DiagnosticsForMember(context, type, memberFlags, memberName, null, null).ToImmutableArray());
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

        public static void FindAndReportForConstructor(GenericContext context, ImmutableArray<string> typeNames, ImmutableArray<ITypeSymbol> paramTypes, MemberFlags memberFlags)
        {
            var diagnostics = new Dictionary<ITypeSymbol, ImmutableArray<Diagnostic>>();
            foreach (var typeName in typeNames)
            {
                var type = FindAndReportForType(context, typeName);
                if (type is null)
                {
                    continue;
                }

                diagnostics.Add(type, DiagnosticsForMember(context, type, memberFlags, string.Empty, paramTypes, null).ToImmutableArray());
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

        public static INamedTypeSymbol? FindAndReportForType(GenericContext context, string typeName)
        {
            var type = context.Compilation.GetAssemblies().Select(a => a.GetTypeByMetadataName(typeName)).FirstOrDefault(t => t is not null);
            if (type is null)
            {
                context.ReportDiagnostic(RuleIdentifiers.ReportType(context, typeName));
                return null;
            }

            return type;
        }


        private static IEnumerable<Diagnostic> DiagnosticsForMember(GenericContext context, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName, ImmutableArray<ITypeSymbol>? paramTypes, ImmutableArray<ArgumentType>? paramVariations)
        {
            if (context.Compilation.GetMetadataReference(typeSymbol.ContainingAssembly) is PortableExecutableReference @ref && File.Exists(@ref.FilePath))
            {
                return CodeMetadataParser.FindMember(context, @ref.FilePath, typeSymbol, memberFlags, memberName, paramTypes, paramVariations);
            }
            else // Fallback to roslyn based check. Mostly used when source code is available within the solution
            {
                return CodeRoslynParser.FindMember(context, typeSymbol, memberFlags, memberName, paramTypes, paramVariations);
            }
        }
    }
}