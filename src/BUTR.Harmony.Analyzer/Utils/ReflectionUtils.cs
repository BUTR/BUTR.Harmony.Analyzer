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
            if (argument.Expression is not TypeOfExpressionSyntax expression) return ImmutableArray<ITypeSymbol>.Empty;

            var type = semanticModel.GetTypeInfo(expression.Type, ct);
            if (type.Type.TypeKind == TypeKind.TypeParameter && type.Type is ITypeParameterSymbol typeParameterSymbol)
            {
                return typeParameterSymbol.ConstraintTypes;
            }
            return ImmutableArray.Create(type.Type);

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
            foreach (var typeDefinition in metadata.TypeDefinitions.Select(metadata.GetTypeDefinition))
            {
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

        public static FieldDefinition? FindFieldDefinition(OperationAnalysisContext context, MetadataReader metadata, TypeDefinition typeDef, bool checkBaseTypes, string fieldName)
        {
            return FindMemberDefinition(context, metadata, typeDef, checkBaseTypes,
                x => x.GetFields().Select(metadata.GetFieldDefinition),
                x => metadata.GetString(x.Name),
                fieldName);
        }

        public static PropertyDefinition? FindPropertyDefinition(OperationAnalysisContext context, MetadataReader metadata, TypeDefinition typeDef, bool checkBaseTypes, string propertyName)
        {
            return FindMemberDefinition(context, metadata, typeDef, checkBaseTypes,
                definition => definition.GetProperties().Select(metadata.GetPropertyDefinition),
                definition => metadata.GetString(definition.Name),
                propertyName);
        }

        public static MethodDefinition? FindMethodDefinition(OperationAnalysisContext context, MetadataReader metadata, TypeDefinition typeDef, bool checkBaseTypes, string methodName)
        {
            return FindMemberDefinition(context, metadata, typeDef, checkBaseTypes,
                definition => definition.GetMethods().Select(metadata.GetMethodDefinition),
                definition => metadata.GetString(definition.Name),
                methodName);
        }

        private static TMember? FindMemberDefinition<TMember>(
            OperationAnalysisContext context,
            MetadataReader metadata,
            TypeDefinition typeDef,
            bool checkBaseTypes,
            Func<TypeDefinition, IEnumerable<TMember>> getMembersFromTypeDef,
            Func<TMember, string> getMemberName,
            string memberName) where TMember : struct
        {
            while (true)
            {
                foreach (var methodDef in getMembersFromTypeDef(typeDef))
                {
                    var methodDefName = getMemberName(methodDef);
                    if (string.Equals(methodDefName, memberName, StringComparison.Ordinal))
                    {
                        return methodDef;
                    }
                }

                if (!checkBaseTypes || typeDef.BaseType.IsNil) break;

                if (typeDef.BaseType.Kind == HandleKind.TypeDefinition)
                {
                    typeDef = metadata.GetTypeDefinition((TypeDefinitionHandle) typeDef.BaseType);
                    continue;
                }

                if (typeDef.BaseType.Kind == HandleKind.TypeReference)
                {
                    var baseTypeRef = metadata.GetTypeReference((TypeReferenceHandle) typeDef.BaseType);
                    var (baseTypeDefNullable, peReader) = FindTypeDefinitionFromTypeReference(context, metadata, baseTypeRef);
                    using var reader = peReader;
                    var baseMetadata = reader.GetMetadataReader();
                    return baseTypeDefNullable is { } baseTypeDef
                        ? FindMemberDefinition(context, baseMetadata, baseTypeDef, checkBaseTypes, getMembersFromTypeDef, getMemberName, memberName)
                        : null;
                }
            }
            return null;
        }

        private static (TypeDefinition?, PEReader) FindTypeDefinitionFromTypeReference(OperationAnalysisContext context, MetadataReader metadata, TypeReference typeReference)
        {
            var typeRefNamespace = metadata.GetString(typeReference.Namespace);
            var typeRefName = metadata.GetString(typeReference.Name);

            var asm = metadata.GetAssemblyReference((AssemblyReferenceHandle) typeReference.ResolutionScope);
            var asmName = metadata.GetString(asm.Name);
            var assembly = GetAssemblies(context).FirstOrDefault(a => a.Name == asmName);
            var typeSymbolRef = context.Compilation.GetMetadataReference(assembly);
            if (typeSymbolRef is not PortableExecutableReference @ref || !File.Exists(@ref.FilePath)) return default;
            var peReaderObject = new PEReader(File.ReadAllBytes(@ref.FilePath).ToImmutableArray());
            var metadataObject = peReaderObject.GetMetadataReader();
            var typeDef = metadataObject.TypeDefinitions.Select(metadataObject.GetTypeDefinition).FirstOrDefault(typeDef =>
            {
                var @namespace = metadataObject.GetString(typeDef.Namespace);
                var name = metadataObject.GetString(typeDef.Name);
                return @namespace == typeRefNamespace && name == typeRefName;
            });
            return (typeDef, peReaderObject);
        }
    }
}