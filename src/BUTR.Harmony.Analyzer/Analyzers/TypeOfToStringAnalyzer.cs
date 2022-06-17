using BUTR.Harmony.Analyzer.Data;
using BUTR.Harmony.Analyzer.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using System;
using System.Collections.Immutable;
using System.Linq;

namespace BUTR.Harmony.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TypeOfToStringAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            RuleIdentifiers.TypeOfToStringRule
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
            if (context.Compilation.GetTypeByMetadataName(typeof(Type).FullName) is not { } typeSymbol) return;
            if (context.Compilation.GetTypeByMetadataName(typeof(string).FullName) is not { } stringSymbol) return;
            if (context.Operation is not IInvocationOperation { Syntax: InvocationExpressionSyntax invocationExpressionSyntax } invocationOperation) return;
            if (!invocationOperation.TargetMethod.ContainingType.Name.StartsWith("AccessTools", StringComparison.Ordinal)) return;
            if (invocationOperation.TargetMethod.Parameters.Length < 2) return;
            if (!invocationOperation.TargetMethod.Parameters[0].Type.Equals(typeSymbol, SymbolEqualityComparer.Default)) return;
            if (!invocationOperation.TargetMethod.Parameters[1].Type.Equals(stringSymbol, SymbolEqualityComparer.Default)) return;

            var typeInfos = RoslynHelper.GetTypeInfos(invocationOperation.SemanticModel, invocationExpressionSyntax.ArgumentList.Arguments[0], context.CancellationToken);
            if (typeInfos.IsEmpty) return;

            var ctx = new GenericContext(context.Compilation, () => invocationExpressionSyntax.ArgumentList.Arguments[0].GetLocation(), context.ReportDiagnostic);
            ctx.ReportDiagnostic(RuleIdentifiers.ReportTypeOfToString(ctx, NameFormatter.ReflectionName(typeInfos.First())));
        }
    }
}