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
            if (context.Operation is not IInvocationOperation invocationOperation) return;
            if (!invocationOperation.TargetMethod.ContainingType.Name.StartsWith("SymbolExtensions", StringComparison.Ordinal)) return;
            if (invocationOperation.Syntax is not InvocationExpressionSyntax invocationExpressionSyntax) return;
            if (invocationExpressionSyntax.ArgumentList.Arguments.Count != 1) return;
            
            if (invocationExpressionSyntax.ArgumentList.Arguments[0].Expression is not LambdaExpressionSyntax lambdaExpressionSyntax) return;
            switch (lambdaExpressionSyntax.Body)
            {
                case MemberAccessExpressionSyntax:
                    AnalyzeFieldProperties(context, invocationOperation, lambdaExpressionSyntax);
                    break;
                case InvocationExpressionSyntax:
                    AnalyzeMethod(context, invocationOperation, lambdaExpressionSyntax);
                    break;
            }
        }
        
        private static void AnalyzeFieldProperties(OperationAnalysisContext context, IInvocationOperation invocationOperation, LambdaExpressionSyntax lambdaExpressionSyntax)
        {
            if (invocationOperation.Syntax is not InvocationExpressionSyntax invocationExpressionSyntax) return;
            
            if (lambdaExpressionSyntax.Body is not MemberAccessExpressionSyntax memberAccessExpressionSyntax) return;
            var typeInfo = invocationOperation.SemanticModel.GetTypeInfo(memberAccessExpressionSyntax);
    
            var ctx = new GenericContext(context.Compilation, () => invocationExpressionSyntax.ArgumentList.Arguments[0].GetLocation(), context.ReportDiagnostic);
            ctx.ReportDiagnostic(RuleIdentifiers.ReportSymbolExtensionsToString(ctx, NameFormatter.ReflectionName(typeInfo.Type)));
        }
        
        private static void AnalyzeMethod(OperationAnalysisContext context, IInvocationOperation invocationOperation, LambdaExpressionSyntax lambdaExpressionSyntax)
        {
            if (invocationOperation.Syntax is not InvocationExpressionSyntax invocationExpressionSyntax) return;
            
            if (lambdaExpressionSyntax.Body is not InvocationExpressionSyntax lambdaInvocationExpressionSyntax) return;
            if (lambdaInvocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax methodMemberAccessExpressionSyntax) return;
            if (methodMemberAccessExpressionSyntax.Expression is not MemberAccessExpressionSyntax typeMemberAccessExpressionSyntax) return;
            var typeInfo = invocationOperation.SemanticModel.GetTypeInfo(typeMemberAccessExpressionSyntax);

            var ctx = new GenericContext(context.Compilation, () => invocationExpressionSyntax.ArgumentList.Arguments[0].GetLocation(), context.ReportDiagnostic);
            ctx.ReportDiagnostic(RuleIdentifiers.ReportSymbolExtensionsToString(ctx, NameFormatter.ReflectionName(typeInfo.Type)));
        }
    }
}