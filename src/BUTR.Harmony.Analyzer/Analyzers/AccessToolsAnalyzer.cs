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
    public class AccessToolsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            MemberUtils.MemberRule,
            MemberUtils.AssemblyRule,
            MemberUtils.TypeRule,
            MemberUtils.PropertyGetterRule,
            MemberUtils.PropertySetterRule,
            FieldRefAccessUtils.WrongTypeRule
        );

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

            var methodName = operation.TargetMethod.Name.AsSpan();

            var flags = MemberFlags.None;

            if (methodName.StartsWith("Declared".AsSpan()))
            {
                flags |= MemberFlags.Declared;
                methodName = methodName.Slice(8);
            }

            if (methodName.StartsWith("Property".AsSpan(), StringComparison.Ordinal))
            {
                flags |= MemberFlags.Property;
                methodName = methodName.Slice(8);

                if (!methodName.IsEmpty)
                {
                    if (methodName.Equals("Setter".AsSpan(), StringComparison.Ordinal))
                        flags |= MemberFlags.Setter;
                    else if (methodName.Equals("Getter".AsSpan(), StringComparison.Ordinal))
                        flags |= MemberFlags.Getter;
                }

                AnalyzeMembers(context, operation, invocation, flags);
            }
            else if (methodName.Equals("Field".AsSpan(), StringComparison.Ordinal))
            {
                flags |= MemberFlags.Field;
                AnalyzeMembers(context, operation, invocation, flags);
            }
            else if (methodName.Equals("Method".AsSpan(), StringComparison.Ordinal))
            {
                flags |= MemberFlags.Method;
                AnalyzeMembers(context, operation, invocation, flags);
            }
            else if (methodName.Equals("StructFieldRefAccess".AsSpan(), StringComparison.Ordinal))
            {
                AnalyzeStructFieldRefAccess(context, operation, invocation);
            }
            else if (methodName.Equals("StaticFieldRefAccess".AsSpan(), StringComparison.Ordinal))
            {
                AnalyzeStaticFieldRefAccess(context, operation, invocation);
            }
        }

        private static void AnalyzeMembers(OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation, MemberFlags memberFlags)
        {
            if (string.Equals(operation.TargetMethod.Parameters.FirstOrDefault()?.Type.Name, nameof(Type), StringComparison.OrdinalIgnoreCase))
            {
                if (invocation.ArgumentList.Arguments.Count < 2) return;

                if (ReflectionUtils.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[1], context.CancellationToken) is not { } methodName) return;
                var typeInfos = ReflectionUtils.GetTypeInfos(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (typeInfos.IsEmpty) return;

                if (typeInfos.Length > 1)
                {
                    MemberUtils.FindAndReportForMembers(context, typeInfos, memberFlags, methodName);
                }
                else
                {
                    MemberUtils.FindAndReportForMember(context, typeInfos[0], memberFlags, methodName);
                }
            }
            else if (string.Equals(operation.TargetMethod.Parameters.FirstOrDefault()?.Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                if (invocation.ArgumentList.Arguments.Count < 1) return;

                var typeSemicolonMember = ReflectionUtils.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (typeSemicolonMember is null) return;

                MemberUtils.FindAndReportForMember(context, typeSemicolonMember, memberFlags);
            }
        }

        private static void AnalyzeStructFieldRefAccess(OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation)
        {
            if (operation.TargetMethod.Arity != 2) return;

            if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                var objectType = operation.TargetMethod.TypeArguments[0];
                var fieldType = operation.TargetMethod.TypeArguments[1];

                var fieldName = ReflectionUtils.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);

                FieldRefAccessUtils.FindAndReportForFieldRefAccess(context, objectType, fieldType, fieldName);
            }

            if (operation.TargetMethod.Parameters.Length == 2 && string.Equals(operation.TargetMethod.Parameters[1].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                var objectType = operation.TargetMethod.TypeArguments[0];
                var fieldType = operation.TargetMethod.TypeArguments[1];

                var fieldName = ReflectionUtils.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);

                FieldRefAccessUtils.FindAndReportForFieldRefAccess(context, objectType, fieldType, fieldName);
            }
        }

        private static void AnalyzeStaticFieldRefAccess(OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation)
        {
            if (operation.TargetMethod.Arity != 2) return;

            if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                var objectType = operation.TargetMethod.TypeArguments[0];
                var fieldType = operation.TargetMethod.TypeArguments[1];

                var fieldName = ReflectionUtils.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);

                FieldRefAccessUtils.FindAndReportForFieldRefAccess(context, objectType, fieldType, fieldName);
            }
        }
    }
}
