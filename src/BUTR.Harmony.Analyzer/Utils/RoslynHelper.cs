﻿using BUTR.Harmony.Analyzer.Data;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class RoslynHelper
    {
        public static IEnumerable<IAssemblySymbol> GetAssemblies(GenericContext context) => context.Compilation.References
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

        public static bool CompareMethodSignatures(IMethodSymbol methodSymbol, ImmutableArray<ITypeSymbol>? paramTypesNullable, ImmutableArray<ArgumentType>? paramVariationsNullable)
        {
            if (paramTypesNullable is not { } paramTypes)
            {
                return true;
            }

            if (methodSymbol.Parameters.Length != paramTypes.Length)
            {
                return false;
            }

            for (var i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                var param = methodSymbol.Parameters[i];
                if (!SymbolEqualityComparer.Default.Equals(param, paramTypes[i]))
                {
                    return false;
                }

                if (paramVariationsNullable is not { } paramVariations)
                {
                    return true;
                }

                if (methodSymbol.Parameters.Length != paramVariations.Length)
                {
                    return false;
                }

                var type = paramVariations[i];
                switch (type)
                {
                    case ArgumentType.Normal:
                    {
                        if (param.RefKind != RefKind.None)
                        {
                            return false;
                        }

                        break;
                    }
                    case ArgumentType.Ref:
                    {
                        if (param.RefKind != RefKind.Ref && param.RefKind != RefKind.RefReadOnly)
                        {
                            return false;
                        }

                        break;
                    }
                    case ArgumentType.Out:
                    {
                        if (param.RefKind != RefKind.Out)
                        {
                            return false;
                        }

                        break;
                    }
                    case ArgumentType.Pointer:
                    {
                        if (param.Type.TypeKind != TypeKind.Pointer)
                        {
                            return false;
                        }

                        break;
                    }
                }
            }

            return true;
        }
    }
}