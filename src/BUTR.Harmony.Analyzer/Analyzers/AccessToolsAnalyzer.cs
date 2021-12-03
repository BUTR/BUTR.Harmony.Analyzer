using BUTR.Harmony.Analyzer.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace BUTR.Harmony.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AccessToolsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MemberParser.MemberRule, MemberParser.AssemblyRule, MemberParser.TypeRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterCompilationStartAction(SetMetadataImportOptions);
        }

        private static void SetMetadataImportOptions(CompilationStartAnalysisContext context)
        {
            context.Compilation.Options.WithMetadataImportOptions(MetadataImportOptions.All);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation) context.Operation;

            if (operation.Syntax is not InvocationExpressionSyntax invocation) return;

            if (!operation.TargetMethod.ContainingType.Name.StartsWith("AccessTools", StringComparison.Ordinal)) return;

            var flags = MemberFlags.None;
            var methodName = operation.TargetMethod.Name.AsSpan();

            if (methodName.StartsWith("Declared".AsSpan()))
            {
                flags |= MemberFlags.Declared;
                methodName = methodName.Slice(8);
            }

            if (methodName.Equals("Property".AsSpan(), StringComparison.Ordinal))
            {
                flags |= MemberFlags.Property;
                Analyze(context, operation, invocation, flags);
            }
            if (methodName.Equals("Field".AsSpan(), StringComparison.Ordinal))
            {
                flags |= MemberFlags.Field;
                Analyze(context, operation, invocation, flags);
            }
            if (methodName.Equals("Method".AsSpan(), StringComparison.Ordinal))
            {
                flags |= MemberFlags.Method;
                Analyze(context, operation, invocation, flags);
            }
        }

        private static void Analyze(OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation, MemberFlags memberFlags)
        {
            if (string.Equals(operation.TargetMethod.Parameters.FirstOrDefault()?.Type.Name, nameof(Type), StringComparison.OrdinalIgnoreCase))
            {
                if (invocation.ArgumentList.Arguments.Count < 2) return;

                if (GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[1], context.CancellationToken) is not { } methodName) return;
                var typeInfos = GetTypeInfos(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (typeInfos.IsEmpty) return;

                if (typeInfos.Length > 1)
                {
                    MemberParser.FindAndReport(context, typeInfos, memberFlags, methodName);
                }
                else
                {
                    MemberParser.FindAndReport(context, typeInfos[0], memberFlags, methodName);
                }
            }
            else if (string.Equals(operation.TargetMethod.Parameters.FirstOrDefault()?.Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                if (invocation.ArgumentList.Arguments.Count < 1) return;

                var typeSemicolonMember = GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (typeSemicolonMember is null) return;

                MemberParser.FindAndReport(context, typeSemicolonMember, memberFlags);
            }
        }

        private static ImmutableArray<ITypeSymbol> GetTypeInfos(SemanticModel? semanticModel, ArgumentSyntax argument, CancellationToken ct)
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
        private static string? GetString(SemanticModel? semanticModel, ArgumentSyntax argument, CancellationToken ct)
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
    }
}
