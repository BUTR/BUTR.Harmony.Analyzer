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
    public class TypeOfToStringFullNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            RuleIdentifiers.TypeOfToStringFullNamedRule
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(SetMetadataImportOptions);
        }

        private void SetMetadataImportOptions(CompilationStartAnalysisContext context)
        {
            context.Compilation.Options.WithMetadataImportOptions(MetadataImportOptions.All);
            context.RegisterOperationAction(AnalyzeInvocationSuggestions, OperationKind.Invocation);
        }

        private void AnalyzeInvocationSuggestions(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation invocationOperation) return;

            if (!invocationOperation.TargetMethod.ContainingType.Name.StartsWith("AccessTools", StringComparison.Ordinal)) return;

            if (invocationOperation.Syntax is not InvocationExpressionSyntax invocationExpressionSyntax) return;

            if (invocationOperation.TargetMethod.Parameters.Length < 2) return;
            
            if (!invocationOperation.TargetMethod.Parameters[0].Type.Name.Equals(nameof(Type), StringComparison.OrdinalIgnoreCase)) return;
            
            if (!invocationOperation.TargetMethod.Parameters[1].Type.Name.Equals(nameof(String), StringComparison.OrdinalIgnoreCase)) return;
            
            var typeInfos = RoslynHelper.GetTypeInfos(invocationOperation.SemanticModel, invocationExpressionSyntax.ArgumentList.Arguments[0], context.CancellationToken);
            if (typeInfos.IsEmpty) return;
            
            var ctx = new GenericContext(context.Compilation, () => invocationOperation.Syntax.GetLocation(), diagnostic => context.ReportDiagnostic(diagnostic));
            ctx.ReportDiagnostic(RuleIdentifiers.ReportTypeOfToStringFullName(ctx, NameFormatter.ReflectionName(typeInfos.First())));
        }
    }
}