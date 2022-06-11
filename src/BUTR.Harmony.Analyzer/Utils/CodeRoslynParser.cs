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
            memberFlags &= ~MemberFlags.Delegate;
            
            var checkBase = !memberFlags.HasFlag(MemberFlags.Declared);
            memberFlags &= ~MemberFlags.Declared;

            var searchType = typeSymbol;
            while (true)
            {
                var result = FindTypeMember(context, searchType, memberFlags, memberName, paramTypes, paramVariations).ToImmutableArray();
                if (result.Length == 0)
                {
                    yield break;
                }

                if (checkBase && searchType.BaseType is { } baseType)
                {
                    searchType = baseType;
                    continue;
                }
                else
                {
                    break;
                }
            }

            // We haven't found the member in the exact type or base classes. Report that.
            yield return RuleIdentifiers.ReportMember(context, NameFormatter.ReflectionName(typeSymbol), memberName);
        }

        private static IEnumerable<Diagnostic> FindTypeMember(GenericContext context, ITypeSymbol typeSymbol, MemberFlags memberFlags, string memberName, ImmutableArray<ITypeSymbol>? paramTypes, ImmutableArray<ArgumentType>? paramVariations)
        {
            var checkGetter = memberFlags.HasFlag(MemberFlags.Getter);
            var checkSetter = memberFlags.HasFlag(MemberFlags.Setter);
            memberFlags &= ~MemberFlags.Getter & ~MemberFlags.Setter;

            var checkConstructor = memberFlags.HasFlag(MemberFlags.Constructor);
            var checkStaticConstructor = memberFlags.HasFlag(MemberFlags.StaticConstructor);
            memberFlags &= ~MemberFlags.Constructor & ~MemberFlags.StaticConstructor;

            if (checkConstructor)
            {
                memberName = ".ctor";
                memberFlags |= MemberFlags.Method;
            }
            if (checkStaticConstructor)
            {
                memberName = ".cctor";
                memberFlags |= MemberFlags.Method;
            }

            foreach (var member in typeSymbol.GetMembers(memberName))
            {
                switch (memberFlags)
                {
                    case MemberFlags.Field when member is IFieldSymbol fieldSymbol:
                    {
                        yield break;
                    }
                    case MemberFlags.Property when member is IPropertySymbol propertySymbol:
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
                    case MemberFlags.Method when member is IMethodSymbol methodSymbol:
                    {
                        if (!RoslynHelper.CompareMethodSignatures(methodSymbol, paramTypes, paramVariations))
                        {
                            continue;
                        }

                        if (checkConstructor && methodSymbol.MethodKind != MethodKind.Constructor)
                        {
                            yield return RuleIdentifiers.ReportMissingConstructor(context, NameFormatter.ReflectionName(typeSymbol));
                        }
                        if (checkStaticConstructor && methodSymbol.MethodKind != MethodKind.StaticConstructor)
                        {
                            yield return RuleIdentifiers.ReportMissingStaticConstructor(context, NameFormatter.ReflectionName(typeSymbol));
                        }

                        yield break;
                    }
                    case MemberFlags.Field:
                    case MemberFlags.Property:
                    case MemberFlags.Method:
                    {
                        continue;
                    }
                }
            }

            yield return RuleIdentifiers.ReportMember(context, NameFormatter.ReflectionName(typeSymbol), memberName);
        }

        public static IEnumerable<Diagnostic> FindMemberAndCheckType(GenericContext context, ITypeSymbol objectType, ITypeSymbol fieldType, string fieldName)
        {
            while (true)
            {
                var foundMembers = objectType.GetMembers(fieldName);
                foreach (var member in foundMembers)
                {
                    if (member is not IFieldSymbol fieldSymbol)
                    {
                        yield return RuleIdentifiers.ReportMember(context, NameFormatter.ReflectionName(objectType), fieldName);
                        yield break;
                    }

                    var fieldTypeName = NameFormatter.ReflectionName(fieldType);
                    var fieldSymbolName = NameFormatter.ReflectionName(fieldSymbol.Type);
                    if (!string.Equals(fieldTypeName, fieldSymbolName))
                    {
                        yield return RuleIdentifiers.ReportWrongType(context, NameFormatter.ReflectionName(objectType), fieldTypeName, fieldSymbolName);
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
            yield return RuleIdentifiers.ReportMember(context, NameFormatter.ReflectionName(objectType), fieldName);
        }
    }
}