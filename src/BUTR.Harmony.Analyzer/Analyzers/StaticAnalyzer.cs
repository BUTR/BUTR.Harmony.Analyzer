using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using System.Collections.Immutable;

namespace BUTR.Harmony.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StaticAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            RuleIdentifiers.NotInstanceFieldRule,
            RuleIdentifiers.NotStaticFieldRule
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
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }
        
        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation invocationOperation) return;

            foreach (var diagnotic in InvocationAnalytics.AnalyzeInvocationAndReport(context, invocationOperation))
            {
                context.ReportDiagnostic(diagnotic);
            }
        }
    }
}