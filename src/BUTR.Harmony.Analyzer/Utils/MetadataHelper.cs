using BUTR.Harmony.Analyzer.Data;
using BUTR.Harmony.Analyzer.Services;

using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class MetadataHelper
    {
        public static (TypeDefinition, PEReader)? FindTypeDefinition(Compilation compilation, PEReader reader, ITypeSymbol typeSymbol)
        {
            var metadata = reader.GetMetadataReader();

            var reflectionTypeName = $"{typeSymbol.ContainingNamespace}.{typeSymbol.MetadataName}";

            foreach (var typeDefinition in metadata.TypeDefinitions.Select(metadata.GetTypeDefinition))
            {
                var typeDefName = $"{metadata.GetString(typeDefinition.Namespace)}.{metadata.GetString(typeDefinition.Name)}";
                if (string.Equals(typeDefName, reflectionTypeName, StringComparison.Ordinal))
                {
                    return (typeDefinition, reader);
                }
            }

            foreach (var typeReference in metadata.TypeReferences.Select(metadata.GetTypeReference))
            {
                var typeRefName = $"{metadata.GetString(typeReference.Namespace)}.{metadata.GetString(typeReference.Name)}";
                if (string.Equals(typeRefName, reflectionTypeName, StringComparison.Ordinal))
                {
                    return FindTypeDefinitionFromTypeReference(compilation, metadata, typeReference);
                }
            }

            return null;
        }
        
        public static (FieldDefinition, PEReader)? FindFieldDefinition(Compilation compilation, PEReader reader, TypeDefinition typeDef, bool checkBaseTypes, string fieldName)
        {
            return FindMemberDefinition(compilation, reader, typeDef, checkBaseTypes,
                (r, x) => x.GetFields().Select(r.GetMetadataReader().GetFieldDefinition),
                fieldName, null, null);
        }

        public static (PropertyDefinition, PEReader)? FindPropertyDefinition(Compilation compilation, PEReader reader, TypeDefinition typeDef, bool checkBaseTypes, string propertyName)
        {
            return FindMemberDefinition(compilation, reader, typeDef, checkBaseTypes,
                (r, x) => x.GetProperties().Select(r.GetMetadataReader().GetPropertyDefinition),
                propertyName, null, null);
        }

        public static (MethodDefinition, PEReader)? FindMethodDefinition(Compilation compilation, PEReader reader, TypeDefinition typeDef, bool checkBaseTypes, string methodName, ImmutableArray<ITypeSymbol>? paramTypes, ImmutableArray<ArgumentType>? paramVariations)
        {
            return FindMemberDefinition(compilation, reader, typeDef, checkBaseTypes,
                (r, x) => x.GetMethods().Select(r.GetMetadataReader().GetMethodDefinition),
                methodName, paramTypes, paramVariations);
        }

        private static (TMember, PEReader)? FindMemberDefinition<TMember>(
            Compilation compilation,
            PEReader reader,
            TypeDefinition typeDef,
            bool checkBaseTypes,
            Func<PEReader, TypeDefinition, IEnumerable<TMember>> getMembersFromTypeDef,
            string memberName, ImmutableArray<ITypeSymbol>? paramTypesNull, ImmutableArray<ArgumentType>? paramVariationsNull) where TMember : struct
        {
            if (paramTypesNull is { } pt && paramVariationsNull is { } pv && pt.Length != pv.Length)
                return null;

            var metadata = reader.GetMetadataReader();
            while (true)
            {
                foreach (var methodDef in getMembersFromTypeDef(reader, typeDef))
                {
                    switch (methodDef)
                    {
                        case FieldDefinition fieldDefinition:
                            var fieldDefName = metadata.GetString(fieldDefinition.Name);
                            if (string.Equals(fieldDefName, memberName, StringComparison.Ordinal))
                            {
                                return (methodDef, reader);
                            }
                            break;
                        case PropertyDefinition propertyDefinition:
                            var propertyDefName = metadata.GetString(propertyDefinition.Name);
                            if (string.Equals(propertyDefName, memberName, StringComparison.Ordinal))
                            {
                                return (methodDef, reader);
                            }
                            break;
                        case MethodDefinition methodDefinition:
                            var methodDefName = metadata.GetString(methodDefinition.Name);
                            if (string.Equals(methodDefName, memberName, StringComparison.Ordinal))
                            {
                                if (!CompareMethodSignatures(methodDefinition, paramTypesNull, paramVariationsNull))
                                {
                                    continue;
                                }

                                return (methodDef, reader);
                            }
                            break;
                    }
                }

                if (!checkBaseTypes || typeDef.BaseType.IsNil)
                {
                    break;
                }

                if (typeDef.BaseType.Kind == HandleKind.TypeDefinition)
                {
                    typeDef = metadata.GetTypeDefinition((TypeDefinitionHandle) typeDef.BaseType);
                    continue;
                }

                if (typeDef.BaseType.Kind == HandleKind.TypeReference)
                {
                    var baseTypeRef = metadata.GetTypeReference((TypeReferenceHandle) typeDef.BaseType);
                    if (FindTypeDefinitionFromTypeReference(compilation, metadata, baseTypeRef) is var (baseTypeDef, baseReader))
                    {
                        typeDef = baseTypeDef;
                        reader = baseReader;
                        metadata = reader.GetMetadataReader();
                        continue;
                    }

                    return null;
                }
            }

            return null;
        }

        public static bool CompareTypes(SignatureType signatureType, ITypeSymbol typeSymbol)
        {
            if (signatureType.IsGeneric)
            {
                if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
                {
                    return false;
                }

                if (namedTypeSymbol.TypeParameters.Length != signatureType.GenericParameters.Length)
                {
                    return false;
                }

                var paramStr = NameFormatter.ReflectionGenericName(signatureType);
                var paramTypeStr = NameFormatter.ReflectionName(namedTypeSymbol);
                if (!string.Equals(paramStr, paramTypeStr))
                {
                    return false;
                }

                for (var i = 0; i < signatureType.GenericParameters.Length; i++)
                {
                    if (!CompareTypes(signatureType.GenericParameters[i], namedTypeSymbol.TypeArguments[i]))
                    {
                        return false;
                    }
                }
            }
            else
            {
                var paramStr = NameFormatter.ReflectionName(signatureType);
                var paramTypeStr = NameFormatter.ReflectionName(typeSymbol);
                if (!string.Equals(paramStr, paramTypeStr))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareMethodSignatures(MethodDefinition methodDefinition, ImmutableArray<ITypeSymbol>? paramTypesNullable, ImmutableArray<ArgumentType>? paramVariationsNullable)
        {
            if (paramTypesNullable is not { } paramTypes)
            {
                return true;
            }

            var sig = methodDefinition.DecodeSignature(new DisassemblingTypeProvider(), DisassemblingGenericContext.Empty);

            if (sig.ParameterTypes.Length != paramTypes.Length)
            {
                return false;
            }

            for (var i = 0; i < sig.ParameterTypes.Length; i++)
            {
                var param = sig.ParameterTypes[i];

                if (!CompareTypes(param, paramTypes[i]))
                {
                    return false;
                }

                if (paramVariationsNullable is not { } paramVariations)
                {
                    return true;
                }

                if (sig.ParameterTypes.Length != paramVariations.Length)
                {
                    return false;
                }

                switch (paramVariations[i])
                {
                    case ArgumentType.Normal:
                    {
                        if (param.IsRef || param.IsPointer)
                        {
                            return false;
                        }
                        break;
                    }
                    case ArgumentType.Ref:
                    {
                        if (!param.IsRef)
                        {
                            return false;
                        }
                        break;
                    }
                    case ArgumentType.Out:
                    {
                        // TODO: At this low level we don't know whether 'out' or 'ref' is used
                        if (!param.IsRef)
                        {
                            return false;
                        }
                        break;
                    }
                    case ArgumentType.Pointer:
                    {
                        if (!param.IsPointer)
                        {
                            return false;
                        }
                        break;
                    }
                }
            }

            return true;
        }

        private static (TypeDefinition, PEReader)? FindTypeDefinitionFromTypeReference(Compilation compilation, MetadataReader metadata, TypeReference typeReference)
        {
            var asm = metadata.GetAssemblyReference((AssemblyReferenceHandle) typeReference.ResolutionScope);
            var asmName = metadata.GetString(asm.Name);
            var assembly = compilation.GetAssemblies().FirstOrDefault(a => a.Name == asmName);

            if (compilation.GetMetadataReference(assembly) is not PortableExecutableReference @ref || !File.Exists(@ref.FilePath))
            {
                return null;
            }

            var peReaderType = new PEReader(File.ReadAllBytes(@ref.FilePath).ToImmutableArray());
            var metadataType = peReaderType.GetMetadataReader();

            var typeRefNamespace = metadata.GetString(typeReference.Namespace);
            var typeRefName = metadata.GetString(typeReference.Name);

            var nullableTypeDef = metadataType.TypeDefinitions.Select(metadataType.GetTypeDefinition).Cast<TypeDefinition?>().FirstOrDefault(typeDef =>
            {
                var @namespace = metadataType.GetString(typeDef!.Value.Namespace);
                var name = metadataType.GetString(typeDef!.Value.Name);
                return @namespace == typeRefNamespace && name == typeRefName;
            });

            if (nullableTypeDef is { } typeDef)
                return (typeDef, peReaderType);

            return null;
        }
    }
}