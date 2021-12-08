using BUTR.Harmony.Analyzer.Data;
using BUTR.Harmony.Analyzer.Services;

using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class CodeMetadataParser
    {
        public static IEnumerable<Diagnostic> FindMember(GenericContext context, string filePath, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName, ImmutableArray<ITypeSymbol>? paramTypes, ImmutableArray<ArgumentType>? paramVariations)
        {
            var disposables = new List<IDisposable>();
            try
            {
                var typeName = NameFormatter.ReflectionName(typeSymbol);

                var checkBase = !memberFlags.HasFlag(MemberFlags.Declared);
                memberFlags &= ~MemberFlags.Declared;

                var checkGetter = memberFlags.HasFlag(MemberFlags.Getter);
                var checkSetter = memberFlags.HasFlag(MemberFlags.Setter);
                memberFlags &= ~MemberFlags.Getter & ~MemberFlags.Setter;

                var checkConstructor = memberFlags.HasFlag(MemberFlags.Constructor);
                var checkStaticConstructor = memberFlags.HasFlag(MemberFlags.StaticConstructor);
                memberFlags &= ~MemberFlags.Constructor & ~MemberFlags.StaticConstructor;

                var peReader = new PEReader(File.ReadAllBytes(filePath).ToImmutableArray());
                disposables.Add(peReader);
                if (!peReader.HasMetadata)
                {
                    yield return RuleIdentifiers.ReportAssembly(context, typeSymbol.ContainingAssembly.Name, typeName);
                    yield break;
                }

                if (MetadataHelper.FindTypeDefinition(context.Compilation, peReader, typeSymbol) is not var (typeDefinition, typeReader))
                {
                    yield return RuleIdentifiers.ReportType(context, typeName);
                    yield break;
                }
                if (disposables.Contains(typeReader)) disposables.Add(typeReader);

                if (memberFlags is MemberFlags.Field)
                {
                    if (MetadataHelper.FindFieldDefinition(context.Compilation, typeReader, typeDefinition, checkBase, memberName) is var (fieldDefinition, fieldReader))
                    {
                        if (disposables.Contains(fieldReader)) disposables.Add(fieldReader);
                    }
                    else
                    {
                        yield return RuleIdentifiers.ReportMember(context, typeName, memberName);
                        yield break;
                    }
                }
                if (memberFlags is MemberFlags.Property)
                {
                    if (MetadataHelper.FindPropertyDefinition(context.Compilation, typeReader, typeDefinition, checkBase, memberName) is var (propertyDefinition, propertyReader))
                    {
                        if (disposables.Contains(propertyReader)) disposables.Add(propertyReader);

                        var accessors = propertyDefinition.GetAccessors();
                        if (checkGetter && accessors.Getter.IsNil)
                        {
                            yield return RuleIdentifiers.ReportMissingGetter(context, memberName);
                        }
                        if (checkSetter && accessors.Setter.IsNil)
                        {
                            yield return RuleIdentifiers.ReportMissingSetter(context, memberName);
                        }
                        yield break;
                    }
                    else
                    {
                        yield return RuleIdentifiers.ReportMember(context, typeName, memberName);
                        yield break;
                    }


                }
                if (memberFlags is MemberFlags.Method)
                {
                    if (checkConstructor)
                    {
                        memberName = ".ctor";
                        checkBase = false;
                    }
                    if (checkStaticConstructor)
                    {
                        memberName = ".cctor";
                        checkBase = false;
                    }

                    if (MetadataHelper.FindMethodDefinition(context.Compilation, typeReader, typeDefinition, checkBase, memberName, paramTypes, paramVariations) is var (methodDefinition, methodReader))
                    {
                        if (disposables.Contains(methodReader)) disposables.Add(methodReader);
                    }
                    else
                    {
                        yield return RuleIdentifiers.ReportMember(context, typeName, memberName);
                        yield break;
                    }
                }
            }
            finally
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }

        public static IEnumerable<Diagnostic> FindMemberAndCheckType(GenericContext context, string filePath, ITypeSymbol objectType, ITypeSymbol fieldType, string fieldName)
        {
            var disposables = new List<IDisposable>();
            try
            {
                var peReader = new PEReader(File.ReadAllBytes(filePath).ToImmutableArray());
                disposables.Add(peReader);

                if (MetadataHelper.FindTypeDefinition(context.Compilation, peReader, objectType) is not var (objectTypeDefinition, objectTypeReader))
                {
                    yield return RuleIdentifiers.ReportType(context, NameFormatter.ReflectionName(objectType));
                    yield break;
                }
                if (disposables.Contains(objectTypeReader)) disposables.Add(objectTypeReader);

                if (MetadataHelper.FindTypeDefinition(context.Compilation, peReader, fieldType) is not var (fieldTypeReference, fieldTypeReader))
                {
                    yield return RuleIdentifiers.ReportType(context, NameFormatter.ReflectionName(fieldType));
                    yield break;
                }
                if (disposables.Contains(fieldTypeReader)) disposables.Add(fieldTypeReader);

                if (MetadataHelper.FindFieldDefinition(context.Compilation, objectTypeReader, objectTypeDefinition, true, fieldName) is not var (objectFieldDefinition, objectFieldReader))
                {
                    yield return RuleIdentifiers.ReportMember(context, NameFormatter.ReflectionName(objectType), fieldName);
                    yield break;
                }
                if (disposables.Contains(objectFieldReader)) disposables.Add(objectFieldReader);

                var fieldDefinitionTypeSignature = objectFieldDefinition.DecodeSignature(new DisassemblingTypeProvider(), DisassemblingGenericContext.Empty);
                var fieldDefinitionTypeName = NameFormatter.ReflectionName(fieldDefinitionTypeSignature);

                var fieldTypeName = NameFormatter.ReflectionName(fieldType);
                if (!fieldTypeName.Equals(fieldDefinitionTypeName, StringComparison.Ordinal))
                {
                    yield return RuleIdentifiers.ReportWrongType(context, NameFormatter.ReflectionName(objectType), fieldTypeName, fieldDefinitionTypeName);
                    yield break;
                }
            }
            finally
            {
                foreach (var disposable in disposables)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}