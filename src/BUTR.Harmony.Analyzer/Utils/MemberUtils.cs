using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class MemberUtils
    {
        public static void FindAndReportForMembers(OperationAnalysisContext context, ImmutableArray<ITypeSymbol> typeSymbols, MemberFlags memberFlags, string memberName)
        {
            var diagnostics = new Dictionary<ITypeSymbol, ImmutableArray<Diagnostic>>();
            foreach (var typeSymbol in typeSymbols)
            {
                diagnostics.Add(typeSymbol, DiagnosticsForMember(context, typeSymbol, memberFlags, memberName).ToImmutableArray());
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

        public static void FindAndReportForMember(OperationAnalysisContext context, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName)
        {
            foreach (var diagnostic in DiagnosticsForMember(context, typeSymbol, memberFlags, memberName))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        public static void FindAndReportForMember(OperationAnalysisContext context, string typeSemicolonMember, MemberFlags memberFlags)
        {
            var split = typeSemicolonMember.Split(':');
            var typeName = split[0];
            var memberName = split[1];

            var type = ReflectionUtils.GetAssemblies(context).Select(a => a.GetTypeByMetadataName(typeName)).FirstOrDefault(t => t is not null);
            if (type is null)
            {
                context.ReportDiagnostic(RuleIdentifiers.ReportType(context.Operation, typeName));
                return;
            }

            foreach (var diagnostic in DiagnosticsForMember(context, type, memberFlags, memberName))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }


        private static IEnumerable<Diagnostic> DiagnosticsForMember(OperationAnalysisContext context, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName)
        {
            var typeSymbolRef = context.Compilation.GetMetadataReference(typeSymbol.ContainingAssembly);
            if (typeSymbolRef is PortableExecutableReference @ref && File.Exists(@ref.FilePath))
            {
                return FindMemberInFileMetadata(context, @ref.FilePath, typeSymbol, memberFlags, memberName);
            }
            else // Fallback to roslyn based finding
            {
                return FindMemberInRoslynMetadata(context, typeSymbol, memberFlags, memberName);
            }
        }

        private static IEnumerable<Diagnostic> FindMemberInFileMetadata(OperationAnalysisContext context, string filePath, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName)
        {
            using var peReader = new PEReader(File.ReadAllBytes(filePath).ToImmutableArray());
            var metadata = peReader.GetMetadataReader();

            var typeName = typeSymbol.ToDisplayString(ReflectionUtils.Style);

            if (ReflectionUtils.FindTypeDefinition(metadata, typeName) is not { } typeDefinition)
            {
                yield return RuleIdentifiers.ReportType(context.Operation, typeName);
                yield break;
            }

            var checkBase = !memberFlags.HasFlag(MemberFlags.Declared);
            if (memberFlags.HasFlag(MemberFlags.Field) && ReflectionUtils.FindFieldDefinition(context, metadata, typeDefinition, checkBase, memberName) is not { })
            {
                yield return RuleIdentifiers.ReportMember(context.Operation, typeName, memberName);
                yield break;
            }
            if (memberFlags.HasFlag(MemberFlags.Property))
            {
                if (ReflectionUtils.FindPropertyDefinition(context, metadata, typeDefinition, checkBase, memberName) is { } propertyDefinition)
                {
                    var accessors = propertyDefinition.GetAccessors();
                    if (memberFlags.HasFlag(MemberFlags.Getter) && accessors.Getter.IsNil)
                    {
                        yield return RuleIdentifiers.ReportMissingGetter(context.Operation, memberName);
                    }
                    if (memberFlags.HasFlag(MemberFlags.Setter) && accessors.Setter.IsNil)
                    {
                        yield return RuleIdentifiers.ReportMissingSetter(context.Operation, memberName);
                    }
                }
                else
                {
                    yield return RuleIdentifiers.ReportMember(context.Operation, typeName, memberName);
                    yield break;
                }


            }
            if (memberFlags.HasFlag(MemberFlags.Method) && ReflectionUtils.FindMethodDefinition(context, metadata, typeDefinition, checkBase, memberName) is not { })
            {
                yield return RuleIdentifiers.ReportMember(context.Operation, typeName, memberName);
                yield break;
            }
        }

        private static IEnumerable<Diagnostic> FindMemberInRoslynMetadata(OperationAnalysisContext context, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName)
        {
            var typeName = typeSymbol.ToDisplayString(ReflectionUtils.Style);

            while (true)
            {
                var foundMembers = typeSymbol.GetMembers(memberName);
                foreach (var member in foundMembers)
                {
                    if (memberFlags.HasFlag(MemberFlags.Field))
                    {
                        if (member is not IFieldSymbol)
                        {
                            yield return RuleIdentifiers.ReportMember(context.Operation, typeName, memberName);
                        }
                        yield break;
                    }
                    if (memberFlags.HasFlag(MemberFlags.Property))
                    {
                        if (member is IPropertySymbol propertySymbol)
                        {
                            if (memberFlags.HasFlag(MemberFlags.Getter) && propertySymbol.GetMethod is null)
                            {
                                yield return RuleIdentifiers.ReportMissingGetter(context.Operation, memberName);
                            }
                            if (memberFlags.HasFlag(MemberFlags.Setter) && propertySymbol.SetMethod is null)
                            {
                                yield return RuleIdentifiers.ReportMissingSetter(context.Operation, memberName);
                            }
                        }
                        yield return RuleIdentifiers.ReportMember(context.Operation, typeName, memberName);
                        yield break;
                    }
                    if (memberFlags.HasFlag(MemberFlags.Method))
                    {
                        if (member is not IMethodSymbol)
                        {
                            yield return RuleIdentifiers.ReportMember(context.Operation, typeName, memberName);
                        }
                        yield break;
                    }
                }

                if (!memberFlags.HasFlag(MemberFlags.Declared) && typeSymbol.BaseType is { } baseType)
                {
                    typeSymbol = baseType;
                    continue;
                }

                break;
            }

            // We haven't found the member in the exact type or base classes. Report that.
            yield return RuleIdentifiers.ReportMember(context.Operation, typeName, memberName);
        }
    }
}