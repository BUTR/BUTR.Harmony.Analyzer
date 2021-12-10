using BUTR.Harmony.Analyzer.Data;
using BUTR.Harmony.Analyzer.Services;

using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
                var checkBase = !memberFlags.HasFlag(MemberFlags.Declared) && !memberFlags.HasFlag(MemberFlags.Constructor) && !memberFlags.HasFlag(MemberFlags.StaticConstructor);
                memberFlags &= ~MemberFlags.Declared;

                var checkGetter = memberFlags.HasFlag(MemberFlags.Getter);
                var checkSetter = memberFlags.HasFlag(MemberFlags.Setter);
                memberFlags &= ~MemberFlags.Getter & ~MemberFlags.Setter;

                var checkConstructor = memberFlags.HasFlag(MemberFlags.Constructor);
                var checkStaticConstructor = memberFlags.HasFlag(MemberFlags.StaticConstructor);
                memberFlags &= ~MemberFlags.Constructor & ~MemberFlags.StaticConstructor;

                if (checkConstructor)
                {
                    memberName = ".ctor";
                }
                if (checkStaticConstructor)
                {
                    memberName = ".cctor";
                }

                var peReader = new PEReader(File.ReadAllBytes(filePath).ToImmutableArray());
                disposables.Add(peReader);
                if (!peReader.HasMetadata)
                {
                    yield return RuleIdentifiers.ReportAssembly(context, typeSymbol.ContainingAssembly.Name, NameFormatter.ReflectionName(typeSymbol));
                    yield break;
                }

                if (MetadataHelper.FindTypeDefinition(context.Compilation, peReader, typeSymbol) is not var (typeDefinition, typeReader))
                {
                    yield return RuleIdentifiers.ReportType(context, NameFormatter.ReflectionName(typeSymbol));
                    yield break;
                }
                if (disposables.Contains(typeReader)) disposables.Add(typeReader);

                switch (memberFlags)
                {
                    case MemberFlags.Field when MetadataHelper.FindFieldDefinition(context.Compilation, typeReader, typeDefinition, checkBase, memberName) is var (fieldDefinition, fieldReader):
                    {
                        if (disposables.Contains(fieldReader)) disposables.Add(fieldReader);
                        yield break;
                    }

                    case MemberFlags.Property when MetadataHelper.FindPropertyDefinition(context.Compilation, typeReader, typeDefinition, checkBase, memberName) is var (propertyDefinition, propertyReader):
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
                    case MemberFlags.Method when MetadataHelper.FindMethodDefinition(context.Compilation, typeReader, typeDefinition, checkBase, memberName, paramTypes, paramVariations) is var (methodDefinition, methodReader):
                    {
                        if (disposables.Contains(methodReader)) disposables.Add(methodReader);
                        yield break;
                    }
                    case MemberFlags.Property:
                    case MemberFlags.Field:
                    case MemberFlags.Method:
                    {
                        yield return RuleIdentifiers.ReportMember(context, NameFormatter.ReflectionName(typeSymbol), memberName);
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

                //if (MetadataHelper.FindTypeDefinition(context.Compilation, peReader, fieldType) is not var (fieldTypeReference, fieldTypeReader))
                //{
                //    yield return RuleIdentifiers.ReportType(context, NameFormatter.ReflectionName(fieldType));
                //    yield break;
                //}
                //if (disposables.Contains(fieldTypeReader)) disposables.Add(fieldTypeReader);

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