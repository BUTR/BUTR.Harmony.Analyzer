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
        public static (TypeDefinition, PEReader)? FindTypeDefinition(GenericContext genericContext, PEReader reader, ITypeSymbol typeSymbol)
        {
            var metadata = reader.GetMetadataReader();

            var reflectionTypeName = $"{typeSymbol.ContainingNamespace}.{typeSymbol.MetadataName}";
            foreach (var typeDefinition in metadata.TypeDefinitions.Select(metadata.GetTypeDefinition))
            {
                var typeDefName = metadata.GetString(typeDefinition.Name);
                var typeDefNamespace = metadata.GetString(typeDefinition.Namespace);
                var typeDefNameFull = $"{typeDefNamespace}.{typeDefName}";
                if (string.Equals(typeDefNameFull, reflectionTypeName, StringComparison.Ordinal))
                {
                    return (typeDefinition, reader);
                }
            }
            foreach (var typeReference in metadata.TypeReferences.Select(metadata.GetTypeReference))
            {
                var typeRefName = metadata.GetString(typeReference.Name);
                var typeRefNamespace = metadata.GetString(typeReference.Namespace);
                var typeRefNameFull = $"{typeRefNamespace}.{typeRefName}";
                if (string.Equals(typeRefNameFull, reflectionTypeName, StringComparison.Ordinal))
                {
                    return FindTypeDefinitionFromTypeReference(genericContext, metadata, typeReference);
                }
            }
            /*
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.Arity > 0)
            {
                var typeParams = namedTypeSymbol.TypeParameters;
            }
            else
            {

            }

            var typeReference = ReflectionHelper.ParseReflectionName(reflectionTypeName);
            foreach (var typeDefinition in metadata.TypeDefinitions.Select(metadata.GetTypeDefinition))
            {
                var typeDefFullName = typeDefinition.GetFullTypeName(metadata);
                if (ReflectionHelper.ParseReflectionName(typeDefFullName.ReflectionName) is not GetClassTypeReference typeDefReferenceGC)
                {
                    var t = ReflectionHelper.ParseReflectionName(typeDefFullName.ReflectionName);
                    continue;
                }

                if (typeReference is GetClassTypeReference typeReferenceGC)
                {
                    if (typeReferenceGC.FullTypeName.Equals(typeDefReferenceGC.FullTypeName))
                    {
                        return typeDefinition;
                    }
                }

                if (typeReference is ParameterizedTypeReference typeReferenceP)
                {
                    if (typeReferenceP.GenericType is GetClassTypeReference typeReferenceGC_)
                    {
                        if (typeReferenceGC_.FullTypeName.Equals(typeDefReferenceGC.FullTypeName))
                        {
                            return typeDefinition;
                        }
                    }
                }
            }
            */
            return null;
        }

        public static (FieldDefinition, PEReader)? FindFieldDefinition(GenericContext context, PEReader reader, TypeDefinition typeDef, bool checkBaseTypes, string fieldName)
        {
            return FindMemberDefinition(context, reader, typeDef, checkBaseTypes,
                (r, x) => x.GetFields().Select(r.GetMetadataReader().GetFieldDefinition),
                fieldName, null, null);
        }

        public static (PropertyDefinition, PEReader)? FindPropertyDefinition(GenericContext context, PEReader reader, TypeDefinition typeDef, bool checkBaseTypes, string propertyName)
        {
            return FindMemberDefinition(context, reader, typeDef, checkBaseTypes,
                (r, x) => x.GetProperties().Select(r.GetMetadataReader().GetPropertyDefinition),
                propertyName, null, null);
        }

        public static (MethodDefinition, PEReader)? FindMethodDefinition(GenericContext context, PEReader reader, TypeDefinition typeDef, bool checkBaseTypes, string methodName, ImmutableArray<ITypeSymbol>? paramTypes, ImmutableArray<ArgumentType>? paramVariations)
        {
            return FindMemberDefinition(context, reader, typeDef, checkBaseTypes,
                (r, x) => x.GetMethods().Select(r.GetMetadataReader().GetMethodDefinition),
                methodName, paramTypes, paramVariations);
        }

        private static (TMember, PEReader)? FindMemberDefinition<TMember>(
            GenericContext context,
            PEReader reader,
            TypeDefinition typeDef,
            bool checkBaseTypes,
            Func<PEReader, TypeDefinition, IEnumerable<TMember>> getMembersFromTypeDef,
            string memberName, ImmutableArray<ITypeSymbol>? paramTypesNullable, ImmutableArray<ArgumentType>? paramVariationsNullable) where TMember : struct
        {
            var metadata = reader.GetMetadataReader();

            if (paramTypesNullable is { } pt && paramVariationsNullable is { } pv && pt.Length != pv.Length)
                return null;

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
                                if (!CompareMethodSignatures(methodDefinition, paramTypesNullable, paramVariationsNullable))
                                    continue;

                                return (methodDef, reader);
                            }
                            break;
                    }
                }

                if (!checkBaseTypes || typeDef.BaseType.IsNil)
                    break;

                if (typeDef.BaseType.Kind == HandleKind.TypeDefinition)
                {
                    typeDef = metadata.GetTypeDefinition((TypeDefinitionHandle) typeDef.BaseType);
                    continue;
                }

                if (typeDef.BaseType.Kind == HandleKind.TypeReference)
                {
                    var baseTypeRef = metadata.GetTypeReference((TypeReferenceHandle) typeDef.BaseType);
                    if (FindTypeDefinitionFromTypeReference(context, metadata, baseTypeRef) is not var (baseTypeDef, peReader))
                    {
                        return null;
                    }
                    return FindMemberDefinition(context, peReader, baseTypeDef, checkBaseTypes, getMembersFromTypeDef, memberName, paramTypesNullable, paramVariationsNullable);
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

                var paramStr = signatureType.Name;
                var paramTypeStr = NameFormatter.ReflectionName(namedTypeSymbol);
                if (!string.Equals(paramStr, paramTypeStr))
                {
                    return false;
                }

                for (var j = 0; j < signatureType.GenericParameters.Length; j++)
                {
                    if (!CompareTypes(signatureType.GenericParameters[j], namedTypeSymbol.TypeArguments[j]))
                    {
                        return false;
                    }
                }
            }
            else
            {
                var paramStr = signatureType.ToString(true);
                //var paramTypeStr = paramTypes[i].ToDisplayString(Style);
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

                var paramVariation = paramVariations[i];
                switch (paramVariation)
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

        private static (TypeDefinition, PEReader)? FindTypeDefinitionFromTypeReference(GenericContext context, MetadataReader metadata, TypeReference typeReference)
        {
            var typeRefNamespace = metadata.GetString(typeReference.Namespace);
            var typeRefName = metadata.GetString(typeReference.Name);

            var asm = metadata.GetAssemblyReference((AssemblyReferenceHandle) typeReference.ResolutionScope);
            var asmName = metadata.GetString(asm.Name);
            var assembly = RoslynHelper.GetAssemblies(context).FirstOrDefault(a => a.Name == asmName);
            var typeSymbolRef = context.Compilation.GetMetadataReference(assembly);
            if (typeSymbolRef is not PortableExecutableReference @ref || !File.Exists(@ref.FilePath)) return null;
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