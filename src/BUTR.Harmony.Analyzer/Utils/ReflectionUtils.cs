using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class ReflectionUtils
    {
        public static readonly SymbolDisplayFormat Style = new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            kindOptions: SymbolDisplayKindOptions.None,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None,
            memberOptions: SymbolDisplayMemberOptions.None,
            localOptions: SymbolDisplayLocalOptions.None,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
            parameterOptions: SymbolDisplayParameterOptions.None
        );

        public static IEnumerable<IAssemblySymbol> GetAssemblies(OperationAnalysisContext context) => context.Compilation.References
            .Select(mr => context.Compilation.GetAssemblyOrModuleSymbol(mr))
            .OfType<IAssemblySymbol>()
            .Concat(new[] { context.Compilation.Assembly });

        public static ImmutableArray<ITypeSymbol> GetTypeInfos(SemanticModel? semanticModel, ArgumentSyntax argument, CancellationToken ct)
        {
            if (semanticModel is null) return ImmutableArray<ITypeSymbol>.Empty;

            if (argument.Expression is TypeOfExpressionSyntax expression)
            {
                var type = semanticModel.GetTypeInfo(expression.Type, ct);
                if (type.Type.TypeKind == TypeKind.TypeParameter && type.Type is ITypeParameterSymbol typeParameterSymbol)
                {
                    return typeParameterSymbol.ConstraintTypes;
                }
                return ImmutableArray.Create(type.Type);
            }

            return ImmutableArray<ITypeSymbol>.Empty;
        }
        public static string? GetString(SemanticModel? semanticModel, ArgumentSyntax argument, CancellationToken ct)
        {
            if (semanticModel is null) return null;

            if (argument.Expression is LiteralExpressionSyntax literal)
                return literal.Token.ValueText;

            var constantValue = semanticModel.GetConstantValue(argument.Expression, ct);
            if (constantValue.HasValue && constantValue.Value is string constString)
                return constString;

            INamedTypeSymbol? StringType() => semanticModel.Compilation.GetTypeByMetadataName("System.String");
            if (semanticModel.GetSymbolInfo(argument.Expression, ct).Symbol is IFieldSymbol { Name: "Empty" } field && SymbolEqualityComparer.Default.Equals(field.Type, StringType()))
                return "";

            return null;
        }

        public static TypeDefinition? FindTypeDefinition(MetadataReader metadata, string fullTypeName)
        {
            foreach (var typeDefinitionHandle in metadata.TypeDefinitions)
            {
                var typeDefinition = metadata.GetTypeDefinition(typeDefinitionHandle);
                var typeDefNamespace = metadata.GetString(typeDefinition.Namespace);
                var typeDefName = metadata.GetString(typeDefinition.Name);
                var typeDefFullName = typeDefNamespace + "." + typeDefName;
                if (string.Equals(typeDefFullName, fullTypeName, StringComparison.Ordinal))
                {
                    return typeDefinition;
                }
            }
            return null;
        }

        public static FieldDefinition? FindFieldDefinition(OperationAnalysisContext context, MetadataReader metadata, TypeDefinition typeDefinition, bool checkBase, string fieldName)
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

                if (checkBase && !typeDefinition.BaseType.IsNil)
                {
                    if (typeDefinition.BaseType.Kind == HandleKind.TypeDefinition)
                    {
                        typeDefinition = metadata.GetTypeDefinition((TypeDefinitionHandle) typeDefinition.BaseType);
                        continue;
                    }
                    if (typeDefinition.BaseType.Kind == HandleKind.TypeReference)
                    {
                        var baseTypeRef = metadata.GetTypeReference((TypeReferenceHandle) typeDefinition.BaseType);
                        var (baseTypeDefinition, peReader) = FindTypeDefinitionFromTypeReference(context, metadata, baseTypeRef);
                        using (peReader)
                        {
                            var baseMetadataReader = peReader.GetMetadataReader();
                            return baseTypeDefinition is { } baseTypeDefinition2
                                ? FindFieldDefinition(context, baseMetadataReader, baseTypeDefinition2, checkBase, fieldName)
                                : null;
                        }
                    }
                }

                break;
            }
            return null;
        }

        public static PropertyDefinition? FindPropertyDefinition(OperationAnalysisContext context, MetadataReader metadata, TypeDefinition typeDefinition, bool checkBase, string propertyName)
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

                if (checkBase && !typeDefinition.BaseType.IsNil)
                {
                    if (typeDefinition.BaseType.Kind == HandleKind.TypeDefinition)
                    {
                        typeDefinition = metadata.GetTypeDefinition((TypeDefinitionHandle) typeDefinition.BaseType);
                        continue;
                    }
                    if (typeDefinition.BaseType.Kind == HandleKind.TypeReference)
                    {
                        var baseTypeRef = metadata.GetTypeReference((TypeReferenceHandle) typeDefinition.BaseType);
                        var (baseTypeDefinition, peReader) = FindTypeDefinitionFromTypeReference(context, metadata, baseTypeRef);
                        using (peReader)
                        {
                            var baseMetadataReader = peReader.GetMetadataReader();
                            return baseTypeDefinition is { } baseTypeDefinition2
                                ? FindPropertyDefinition(context, baseMetadataReader, baseTypeDefinition2, checkBase, propertyName)
                                : null;
                        }
                    }
                }

                break;
            }
            return null;
        }

        public static MethodDefinition? FindMethodDefinition(OperationAnalysisContext context, MetadataReader metadata, TypeDefinition typeDefinition, bool checkBase, string methodName)
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

                if (checkBase && !typeDefinition.BaseType.IsNil)
                {
                    if (typeDefinition.BaseType.Kind == HandleKind.TypeDefinition)
                    {
                        typeDefinition = metadata.GetTypeDefinition((TypeDefinitionHandle) typeDefinition.BaseType);
                        continue;
                    }
                    if (typeDefinition.BaseType.Kind == HandleKind.TypeReference)
                    {
                        var baseTypeRef = metadata.GetTypeReference((TypeReferenceHandle) typeDefinition.BaseType);
                        var (baseTypeDefinition, peReader) = FindTypeDefinitionFromTypeReference(context, metadata, baseTypeRef);
                        using (peReader)
                        {
                            var baseMetadataReader = peReader.GetMetadataReader();
                            return baseTypeDefinition is { } baseTypeDefinition2
                                ? FindMethodDefinition(context, baseMetadataReader, baseTypeDefinition2, checkBase, methodName)
                                : null;
                        }
                    }
                }

                break;
            }
            return null;
        }

        private static (TypeDefinition?, PEReader) FindTypeDefinitionFromTypeReference(OperationAnalysisContext context, MetadataReader metadata, TypeReference typeReference)
        {
            var baseTypeRefNamespace = metadata.GetString(typeReference.Namespace);
            var baseTypeRefName = metadata.GetString(typeReference.Name);

            var scope = typeReference.ResolutionScope;
            var asm = metadata.GetAssemblyReference((AssemblyReferenceHandle) scope);
            var asmName = metadata.GetString(asm.Name);
            var assembly = GetAssemblies(context).FirstOrDefault(a => a.Name == asmName);
            var typeSymbolRef = context.Compilation.GetMetadataReference(assembly);
            if (typeSymbolRef is PortableExecutableReference @ref && File.Exists(@ref.FilePath))
            {
                var peReaderObject = new PEReader(File.ReadAllBytes(@ref.FilePath).ToImmutableArray());
                var metadataObject = peReaderObject.GetMetadataReader();
                var typeHandle = metadataObject.TypeDefinitions.FirstOrDefault(typeDefHandle =>
                {
                    var typeDef = metadataObject.GetTypeDefinition(typeDefHandle);
                    var @namespace = metadataObject.GetString(typeDef.Namespace);
                    var name = metadataObject.GetString(typeDef.Name);
                    return @namespace == baseTypeRefNamespace && name == baseTypeRefName;
                });
                return (metadataObject.GetTypeDefinition(typeHandle), peReaderObject);
            }
            return default;
        }
    }
}