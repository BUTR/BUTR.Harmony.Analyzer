using BUTR.Harmony.Analyzer.Data;
using BUTR.Harmony.Analyzer.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using System;
using System.Collections.Immutable;

namespace BUTR.Harmony.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SymbolExtensionsToStringAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            RuleIdentifiers.SymbolExtensionsToStringRule
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(SetMetadataImportOptions);
        }

        private static void SetMetadataImportOptions(CompilationStartAnalysisContext context)
        {
            context.Compilation.Options.WithMetadataImportOptions(MetadataImportOptions.All);
            context.RegisterOperationAction(AnalyzeInvocationSuggestions, OperationKind.Invocation);
        }

        private static void AnalyzeInvocationSuggestions(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation { Syntax: InvocationExpressionSyntax invocationExpressionSyntax } invocationOperation) return;
            if (!invocationOperation.TargetMethod.ContainingType.Name.StartsWith("SymbolExtensions", StringComparison.Ordinal)) return;
            if (invocationOperation.TargetMethod.Parameters.Length != 1) return;

            if (invocationExpressionSyntax.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambdaExpressionSyntax) return;
            if (invocationOperation.SemanticModel.GetSymbolInfo(lambdaExpressionSyntax.Body).Symbol is not { } methodSymbol) return;
            if (methodSymbol.ContainingType is null) return;

            var ctx = new GenericContext(context.Compilation, () => invocationExpressionSyntax.ArgumentList.Arguments[0].GetLocation(), context.ReportDiagnostic);
            ctx.ReportDiagnostic(RuleIdentifiers.ReportSymbolExtensionsToString(ctx, NameFormatter.ReflectionName(methodSymbol.ContainingType)));
        }
    }
}