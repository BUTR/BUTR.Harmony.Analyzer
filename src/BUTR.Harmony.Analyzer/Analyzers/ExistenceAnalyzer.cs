using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace BUTR.Harmony.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExistenceAnalyzer : DiagnosticAnalyzer
    {
        private readonly ConcurrentBag<Location> _ignoredLocations = new();

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            RuleIdentifiers.MemberRule,
            RuleIdentifiers.AssemblyRule,
            RuleIdentifiers.TypeRule,
            RuleIdentifiers.PropertyGetterRule,
            RuleIdentifiers.PropertySetterRule,
            RuleIdentifiers.WrongTypeRule,
            RuleIdentifiers.ConstructorRule,
            RuleIdentifiers.StaticConstructorRule
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
            context.RegisterOperationAction(AnalyzeVariableDeclarator, OperationKind.VariableDeclarator);
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            var variableDeclaratorOperation = (IVariableDeclaratorOperation) context.Operation;

            if (variableDeclaratorOperation.GetVariableInitializer() is not { } variableInitializerOperation) return;

            if (variableInitializerOperation.Value is not ICoalesceOperation coalesceOperation) return;

            var diagnostics = new Dictionary<IInvocationOperation, ImmutableArray<Diagnostic>>();
            foreach (var invocationOperation in GetAllInvocations(coalesceOperation))
            {
                diagnostics[invocationOperation] = InvocationAnalytics.AnalyzeInvocationAndReport(context, invocationOperation);
            }

            // If every invocation has an error, display all of them
            // If at least one type found the method, we are correct
            if (diagnostics.All(kv => !kv.Value.IsEmpty))
            {
                foreach (var diagnostic in diagnostics.Values.SelectMany(x => x))
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }

            // Do not analyze the invocations again again
            foreach (var operation in diagnostics.Keys)
            {
                _ignoredLocations.Add(operation.Syntax.GetLocation());
            }
        }

        private static IEnumerable<IInvocationOperation> GetAllInvocations(ICoalesceOperation coalesceOperation)
        {
            foreach (var operationChild in coalesceOperation.Children)
            {
                if (operationChild is IInvocationOperation childInvocationOperand)
                {
                    yield return childInvocationOperand;
                }

                if (operationChild is ICoalesceOperation childCoalesceOperation)
                {
                    foreach (var invocationOperation in GetAllInvocations(childCoalesceOperation))
                    {
                        yield return invocationOperation;
                    }
                }
            }
        }

        // Analyzes all invocations, they can 
        private void AnalyzeInvocation(OperationAnalysisContext context)
        {
            if (context.Operation is not IInvocationOperation invocationOperation) return;

            if (_ignoredLocations.Contains(invocationOperation.Syntax.GetLocation())) return;

            foreach (var diagnotic in InvocationAnalytics.AnalyzeInvocationAndReport(context, invocationOperation))
            {
                context.ReportDiagnostic(diagnotic);
            }
        }
    }
}