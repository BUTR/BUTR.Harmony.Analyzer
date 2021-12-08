using BUTR.Harmony.Analyzer.Data;

using Microsoft.CodeAnalysis;

using System.Collections.Generic;
using System.Collections.Immutable;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class CodeRoslynParser
    {
        public static IEnumerable<Diagnostic> FindMember(GenericContext context, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName, ImmutableArray<ITypeSymbol>? paramTypes, ImmutableArray<ArgumentType>? paramVariations)
        {
            var checkBase = !memberFlags.HasFlag(MemberFlags.Declared);
            memberFlags &= ~MemberFlags.Declared;

            var checkGetter = memberFlags.HasFlag(MemberFlags.Getter);
            var checkSetter = memberFlags.HasFlag(MemberFlags.Setter);
            memberFlags &= ~MemberFlags.Getter & ~MemberFlags.Setter;

            var checkConstructor = memberFlags.HasFlag(MemberFlags.Constructor);
            var checkStaticConstructor = memberFlags.HasFlag(MemberFlags.StaticConstructor);
            memberFlags &= ~MemberFlags.Constructor & ~MemberFlags.StaticConstructor;

            var typeName = NameFormatter.ReflectionName(typeSymbol);

            while (true)
            {
                var foundMembers = typeSymbol.GetMembers(memberName);
                foreach (var member in foundMembers)
                {
                    if (memberFlags is MemberFlags.Field)
                    {
                        if (member is not IFieldSymbol)
                        {
                            yield return RuleIdentifiers.ReportMember(context, typeName, memberName);
                            yield break;
                        }
                    }
                    if (memberFlags is MemberFlags.Property)
                    {
                        if (member is IPropertySymbol propertySymbol)
                        {
                            if (checkGetter && propertySymbol.GetMethod is null)
                            {
                                yield return RuleIdentifiers.ReportMissingGetter(context, memberName);
                            }
                            if (checkSetter && propertySymbol.SetMethod is null)
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

                        if (member is IMethodSymbol methodSymbol && !RoslynHelper.CompareMethodSignatures(methodSymbol, paramTypes, paramVariations))
                        {
                            if (checkConstructor && methodSymbol.MethodKind != MethodKind.Constructor)
                            {
                                yield return RuleIdentifiers.ReportMissingConstructor(context, typeName);
                            }
                            if (checkStaticConstructor && methodSymbol.MethodKind != MethodKind.StaticConstructor)
                            {
                                yield return RuleIdentifiers.ReportMissingStaticConstructor(context, typeName);
                            }

                            continue;
                        }
                        yield return RuleIdentifiers.ReportMember(context, typeName, memberName);
                        yield break;
                    }
                }

                if (checkBase && typeSymbol.BaseType is { } baseType)
                {
                    typeSymbol = baseType;
                    continue;
                }

                break;
            }

            // We haven't found the member in the exact type or base classes. Report that.
            yield return RuleIdentifiers.ReportMember(context, typeName, memberName);
        }

        public static IEnumerable<Diagnostic> FindMemberAndCheckType(GenericContext context, ITypeSymbol objectType, ITypeSymbol fieldType, string fieldName)
        {
            var objectTypeName = NameFormatter.ReflectionName(objectType);

            while (true)
            {
                var foundMembers = objectType.GetMembers(fieldName);
                foreach (var member in foundMembers)
                {
                    if (member is not IFieldSymbol fieldSymbol)
                    {
                        yield return RuleIdentifiers.ReportMember(context, objectTypeName, fieldName);
                        yield break;
                    }

                    var fieldTypeName = NameFormatter.ReflectionName(fieldType);
                    var fieldSymbolName = NameFormatter.ReflectionName(fieldSymbol.Type);
                    if (!string.Equals(fieldTypeName, fieldSymbolName))
                    {
                        yield return RuleIdentifiers.ReportWrongType(context, objectTypeName, fieldTypeName, fieldSymbolName);
                        yield break;
                    }

                    yield break;
                }

                if (true && objectType.BaseType is { } baseType)
                {
                    objectType = baseType;
                    continue;
                }

                break;
            }

            // We haven't found the member in the exact type or base classes. Report that.
            yield return RuleIdentifiers.ReportMember(context, objectTypeName, fieldName);
        }
    }
}