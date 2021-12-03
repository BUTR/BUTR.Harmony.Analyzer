using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class MemberParser
    {
        public static readonly DiagnosticDescriptor AssemblyRule = new(
            RuleIdentifiers.AssemblyNotFound,
            title: "Assembly does not exist for Type",
            messageFormat: "Assembly '{0}' does not exist for Type '{1}'",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.AssemblyNotFound));

        public static readonly DiagnosticDescriptor TypeRule = new(
            RuleIdentifiers.TypeNotFound,
            title: "Type was not found",
            messageFormat: "Type '{0}' was not found",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.TypeNotFound));

        public static readonly DiagnosticDescriptor MemberRule = new(
            RuleIdentifiers.MemberDoesntExists,
            title: "Member does not exist in Type",
            messageFormat: "Member '{0}' does not exist in Type '{1}'",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.MemberDoesntExists));

        public static void FindAndReport(OperationAnalysisContext context, ImmutableArray<ITypeSymbol> typeSymbols, MemberFlags memberFlags, string memberName)
        {
            var diagnostics = new Dictionary<ITypeSymbol, ImmutableArray<Diagnostic>>();
            foreach (var typeSymbol in typeSymbols)
            {
                var symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
                var typeName = typeSymbol.ToDisplayString(symbolDisplayFormat);

                var assemblyIdentity = typeSymbol.ContainingAssembly.Identity;
                if (GetAssemblies(context).FirstOrDefault(a => a.Name == assemblyIdentity?.Name) is not { } assembly)
                {
                    context.ReportDiagnostic(DiagnosticUtils.CreateDiagnostic(AssemblyRule, context.Operation, assemblyIdentity?.Name ?? string.Empty, typeName));
                    return;
                }

                diagnostics.Add(typeSymbol, Find(context, assembly, typeName, memberFlags, memberName).ToImmutableArray());
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

        public static void FindAndReport(OperationAnalysisContext context, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName)
        {
            var symbolDisplayFormat = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
            var typeName = typeSymbol.ToDisplayString(symbolDisplayFormat);

            var assemblyIdentity = typeSymbol.ContainingAssembly.Identity;
            if (GetAssemblies(context).FirstOrDefault(a => a.Name == assemblyIdentity?.Name) is not { } assembly)
            {
                context.ReportDiagnostic(DiagnosticUtils.CreateDiagnostic(AssemblyRule, context.Operation, assemblyIdentity?.Name ?? string.Empty, typeName));
                return;
            }

            foreach (var diagnostic in Find(context, assembly, typeName, memberFlags, memberName))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        public static void FindAndReport(OperationAnalysisContext context, string typeSemicolonMember, MemberFlags memberFlags)
        {
            var split = typeSemicolonMember.Split(':');
            var typeName = split[0];
            var memberName = split[1];

            var type = GetAssemblies(context).Select(a => a.GetTypeByMetadataName(typeName)).FirstOrDefault(t => t is not null);
            if (type is null)
            {
                context.ReportDiagnostic(DiagnosticUtils.CreateDiagnostic(TypeRule, context.Operation, typeName));
                return;
            }

            var assembly = type.ContainingAssembly;

            foreach (var diagnostic in Find(context, assembly, typeName, memberFlags, memberName))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<Diagnostic> Find(OperationAnalysisContext context, IAssemblySymbol assembly, string typeName, MemberFlags memberFlags, string memberName)
        {
            if (context.Compilation.GetMetadataReference(assembly) is PortableExecutableReference reference && File.Exists(reference.FilePath))
            {
                return FindInFileMetadata(context.Operation, reference.FilePath, typeName, memberFlags, memberName);
            }
            else // Fallback to roslyn based finding
            {
                return FindInRoslynMetadata(context.Operation, assembly, typeName, memberFlags, memberName);
            }
        }

        private static IEnumerable<Diagnostic> FindInFileMetadata(IOperation operation, string filePath, string typeName, MemberFlags memberFlags, string memberName)
        {
            static TypeDefinition? FindTypeDefinition(MetadataReader metadata, string fullTypeName)
            {
                foreach (var typeDefinitionHandle in metadata.TypeDefinitions)
                {
                    var typeDefinition = metadata.GetTypeDefinition(typeDefinitionHandle);
                    var typeDefName = metadata.GetString(typeDefinition.Name);
                    var typeDefNamespace = metadata.GetString(typeDefinition.Namespace);
                    var typeDefFullName = typeDefNamespace + "." + typeDefName;
                    if (string.Equals(typeDefFullName, fullTypeName, StringComparison.Ordinal))
                    {
                        return typeDefinition;
                    }
                }
                return null;
            }

            static FieldDefinition? FindFieldDefinition(MetadataReader metadata, TypeDefinition typeDefinition, MemberFlags memberFlags, string fieldName)
            {
                while (true)
                {
                    foreach (var fieldDefinitionHandle in typeDefinition.GetFields())
                    {
                        var fieldDefinition = metadata.GetFieldDefinition(fieldDefinitionHandle);
                        var fieldDefName = metadata.GetString(fieldDefinition.Name);
                        if (string.Equals(fieldDefName, fieldName, StringComparison.Ordinal))
                        {
                            return fieldDefinition;
                        }
                    }

                    if (!memberFlags.HasFlag(MemberFlags.Declared) && typeDefinition.BaseType.Kind == HandleKind.TypeDefinition)
                    {
                        var baseTypeDef = metadata.GetTypeDefinition((TypeDefinitionHandle)typeDefinition.BaseType);
                        typeDefinition = baseTypeDef;
                        continue;
                    }

                    break;
                }
                return null;
            }

            static PropertyDefinition? FindPropertyDefinition(MetadataReader metadata, TypeDefinition typeDefinition, MemberFlags memberFlags, string propertyName)
            {
                while (true)
                {
                    foreach (var propertyDefinitionHandle in typeDefinition.GetProperties())
                    {
                        var propertyDefinition = metadata.GetPropertyDefinition(propertyDefinitionHandle);
                        var propertyDefName = metadata.GetString(propertyDefinition.Name);
                        if (string.Equals(propertyDefName, propertyName, StringComparison.Ordinal))
                        {
                            return propertyDefinition;
                        }
                    }

                    if (!memberFlags.HasFlag(MemberFlags.Declared) && typeDefinition.BaseType.Kind == HandleKind.TypeDefinition)
                    {
                        var baseTypeDef = metadata.GetTypeDefinition((TypeDefinitionHandle) typeDefinition.BaseType);
                        typeDefinition = baseTypeDef;
                        continue;
                    }

                    break;
                }
                return null;
            }

            static MethodDefinition? FindMethodDefinition(MetadataReader metadata, TypeDefinition typeDefinition, MemberFlags memberFlags, string methodName)
            {
                while (true)
                {
                    foreach (var methodDefinitionHandle in typeDefinition.GetMethods())
                    {
                        var methodDefinition = metadata.GetMethodDefinition(methodDefinitionHandle);
                        var methodDefName = metadata.GetString(methodDefinition.Name);
                        if (string.Equals(methodDefName, methodName, StringComparison.Ordinal))
                        {
                            return methodDefinition;
                        }
                    }

                    if (!memberFlags.HasFlag(MemberFlags.Declared) && typeDefinition.BaseType.Kind == HandleKind.TypeDefinition)
                    {
                        var baseTypeDef = metadata.GetTypeDefinition((TypeDefinitionHandle)typeDefinition.BaseType);
                        typeDefinition = baseTypeDef;
                        continue;
                    }

                    break;
                }
                return null;
            }


            using var peReader = new PEReader(File.ReadAllBytes(filePath).ToImmutableArray());
            var metadata = peReader.GetMetadataReader();

            if (FindTypeDefinition(metadata, typeName) is not { } typeDefinition)
            {
                yield return DiagnosticUtils.CreateDiagnostic(TypeRule, operation, typeName);
                yield break;
            }

            if (memberFlags.HasFlag(MemberFlags.Field) && FindFieldDefinition(metadata, typeDefinition, memberFlags, memberName) is not { })
            {
                yield return ReportMember(operation, typeName, memberName);
                yield break;
            }
            if (memberFlags.HasFlag(MemberFlags.Property) && FindPropertyDefinition(metadata, typeDefinition, memberFlags, memberName) is not { })
            {
                yield return ReportMember(operation, typeName, memberName);
                yield break;
            }
            if (memberFlags.HasFlag(MemberFlags.Method) && FindMethodDefinition(metadata, typeDefinition, memberFlags, memberName) is not { })
            {
                yield return ReportMember(operation, typeName, memberName);
                yield break;
            }
        }

        private static IEnumerable<Diagnostic> FindInRoslynMetadata(IOperation operation, IAssemblySymbol assembly, string typeName, MemberFlags memberFlags, string memberName)
        {
            var type = assembly.GetTypeByMetadataName(typeName);
            if (type is null)
            {
                yield return DiagnosticUtils.CreateDiagnostic(TypeRule, operation, typeName);
                yield break;
            }

            while (true)
            {
                var foundMembers = type.GetMembers(memberName);
                foreach (var member in foundMembers)
                {
                    if (memberFlags.HasFlag(MemberFlags.Field))
                    {
                        if (member is not IFieldSymbol)
                        {
                            yield return ReportMember(operation, typeName, memberName);
                        }
                        yield break;
                    }
                    if (memberFlags.HasFlag(MemberFlags.Property))
                    {
                        if (member is not IPropertySymbol)
                        {
                            yield return ReportMember(operation, typeName, memberName);
                        }
                        yield break;
                    }
                    if (memberFlags.HasFlag(MemberFlags.Method))
                    {
                        if (member is not IMethodSymbol)
                        {
                            yield return ReportMember(operation, typeName, memberName);
                        }
                        yield break;
                    }
                }

                if (!memberFlags.HasFlag(MemberFlags.Declared) && type.BaseType is { } baseType)
                {
                    type = baseType;
                    continue;
                }

                break;
            }

            // We haven't found the member in the exact type or base classes. Report that.
            yield return ReportMember(operation, typeName, memberName);
        }

        private static Diagnostic ReportMember(IOperation operation, string typeName, string memberName)
        {
            return DiagnosticUtils.CreateDiagnostic(MemberRule, operation, memberName, typeName);
        }

        private static IEnumerable<IAssemblySymbol> GetAssemblies(OperationAnalysisContext context) => context.Compilation.References
            .Select(mr => context.Compilation.GetAssemblyOrModuleSymbol(mr))
            .OfType<IAssemblySymbol>()
            .Concat(new[] { context.Compilation.Assembly });
    }
}