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
    public class AccessToolsAnalyzer : DiagnosticAnalyzer
    {
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

        private static void SetMetadataImportOptions(CompilationStartAnalysisContext context)
        {
            context.Compilation.Options.WithMetadataImportOptions(MetadataImportOptions.All);
            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var operation = (IInvocationOperation) context.Operation;

            if (operation.Syntax is not InvocationExpressionSyntax invocation) return;

            if (!operation.TargetMethod.ContainingType.Name.StartsWith("AccessTools", StringComparison.Ordinal)) return;

            var methodName = operation.TargetMethod.Name.AsSpan();

            var flags = MemberFlags.None;

            if (methodName.Equals("StructFieldRefAccess".AsSpan(), StringComparison.Ordinal))
            {
                AnalyzeStructFieldRefAccess(context, operation, invocation);
                return;
            }
            else if (methodName.Equals("StaticFieldRefAccess".AsSpan(), StringComparison.Ordinal))
            {
                AnalyzeStaticFieldRefAccess(context, operation, invocation);
                return;
            }
            else if (methodName.Equals("TypeByName".AsSpan(), StringComparison.Ordinal))
            {
                AnalyzeTypeByName(context, operation, invocation);
                return;
            }
            
            if (methodName.StartsWith("Get".AsSpan()))
            {
                methodName = methodName.Slice(3);
            }
            
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
                    if (methodName.StartsWith("Setter".AsSpan(), StringComparison.Ordinal))
                    {
                        flags |= MemberFlags.Setter;
                        methodName = methodName.Slice(6);
                    }
                    else if (methodName.StartsWith("Getter".AsSpan(), StringComparison.Ordinal))
                    {
                        flags |= MemberFlags.Getter;
                        methodName = methodName.Slice(6);
                    }
                }
            }
            if (methodName.StartsWith("Field".AsSpan(), StringComparison.Ordinal))
            {
                flags |= MemberFlags.Field;
                methodName = methodName.Slice(5);
            }
            if (methodName.StartsWith("Method".AsSpan(), StringComparison.Ordinal))
            {
                flags |= MemberFlags.Method;
                methodName = methodName.Slice(6);
            }
            if (methodName.StartsWith("Constructor".AsSpan(), StringComparison.Ordinal))
            {
                flags |= MemberFlags.Constructor;
                methodName = methodName.Slice(11);
            }
            if (methodName.StartsWith("Delegate".AsSpan(), StringComparison.Ordinal))
            {
                flags |= MemberFlags.Delegate;
                methodName = methodName.Slice(8);
            }

            if (!flags.HasFlag(MemberFlags.Field) &&!flags.HasFlag(MemberFlags.Property) && !flags.HasFlag(MemberFlags.Constructor))
                flags |= MemberFlags.Method;

            if (flags.HasFlag(MemberFlags.Constructor))
            {
                AnalyzeConstructor(context, operation, invocation, flags);
                return;
            }

            AnalyzeMembers(context, operation, invocation, flags);
            return;
        }

        private static void AnalyzeConstructor(OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation, MemberFlags memberFlags)
        {
            var ctx = new GenericContext(context.Compilation, () => context.Operation.Syntax.GetLocation(), context.ReportDiagnostic);

            if (string.Equals(operation.TargetMethod.Parameters.FirstOrDefault()?.Type.Name, nameof(Type), StringComparison.OrdinalIgnoreCase))
            {
                if (invocation.ArgumentList.Arguments.Count < 2) return;

                var typeInfos = RoslynHelper.GetTypeInfos(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (typeInfos.IsEmpty) return;

                var paramTypes = ImmutableArray<ITypeSymbol>.Empty;
                if (invocation.ArgumentList.Arguments.Count == 2 && invocation.ArgumentList.Arguments[1].Expression is ArrayCreationExpressionSyntax { Initializer: { } initializer })
                {
                    paramTypes = RoslynHelper.GetTypeInfosFromInitializer(operation.SemanticModel, initializer, context.CancellationToken);
                }
                
                MemberHelper.FindAndReportForConstructor(ctx, typeInfos.Select(NameFormatter.ReflectionName).ToImmutableArray(), paramTypes, memberFlags);
            }
            else if (string.Equals(operation.TargetMethod.Parameters.FirstOrDefault()?.Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                if (invocation.ArgumentList.Arguments.Count < 1) return;

                var typeName = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (typeName is null) return;

                var paramTypes = ImmutableArray<ITypeSymbol>.Empty;
                if (invocation.ArgumentList.Arguments.Count == 2)
                {
                    if (invocation.ArgumentList.Arguments[1].Expression is ArrayCreationExpressionSyntax { Initializer: { } initializer })
                    {
                        paramTypes = RoslynHelper.GetTypeInfosFromInitializer(operation.SemanticModel, initializer, context.CancellationToken);
                    }
                }
                
                MemberHelper.FindAndReportForConstructor(ctx, ImmutableArray.Create<string>(typeName), paramTypes, memberFlags);
            }
        }
        
        private static void AnalyzeMembers(OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation, MemberFlags memberFlags)
        {
            var ctx = new GenericContext(context.Compilation, () => context.Operation.Syntax.GetLocation(), context.ReportDiagnostic);

            if (string.Equals(operation.TargetMethod.Parameters.FirstOrDefault()?.Type.Name, nameof(Type), StringComparison.OrdinalIgnoreCase))
            {
                if (invocation.ArgumentList.Arguments.Count < 2) return;

                if (RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[1], context.CancellationToken) is not { } methodName) return;
                var typeInfos = RoslynHelper.GetTypeInfos(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (typeInfos.IsEmpty) return;

                MemberHelper.FindAndReportForMembers(ctx, typeInfos.Select(x => $"{NameFormatter.ReflectionName(x)}:{methodName}").ToImmutableArray(), memberFlags);
            }
            else if (string.Equals(operation.TargetMethod.Parameters.FirstOrDefault()?.Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                if (invocation.ArgumentList.Arguments.Count < 1) return;

                var typeSemicolonMember = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (typeSemicolonMember is null) return;

                MemberHelper.FindAndReportForMembers(ctx, ImmutableArray.Create<string>(typeSemicolonMember), memberFlags);
            }
        }

        private static void AnalyzeStructFieldRefAccess(OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation)
        {
            var ctx = new GenericContext(context.Compilation, () => context.Operation.Syntax.GetLocation(), context.ReportDiagnostic);

            if (operation.TargetMethod.Arity != 2) return;

            if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                var objectType = operation.TargetMethod.TypeArguments[0];
                var fieldType = operation.TargetMethod.TypeArguments[1];

                var fieldName = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (fieldName is not null)
                    FieldRefAccessHelper.FindAndReportForFieldRefAccess(ctx, objectType, fieldType, fieldName);
            }

            if (operation.TargetMethod.Parameters.Length == 2 && string.Equals(operation.TargetMethod.Parameters[1].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                var objectType = operation.TargetMethod.TypeArguments[0];
                var fieldType = operation.TargetMethod.TypeArguments[1];

                var fieldName = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (fieldName is not null)
                    FieldRefAccessHelper.FindAndReportForFieldRefAccess(ctx, objectType, fieldType, fieldName);
            }
        }

        private static void AnalyzeStaticFieldRefAccess(OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation)
        {
            var ctx = new GenericContext(context.Compilation, () => context.Operation.Syntax.GetLocation(), context.ReportDiagnostic);

            if (operation.TargetMethod.Arity != 2) return;

            if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                var objectType = operation.TargetMethod.TypeArguments[0];
                var fieldType = operation.TargetMethod.TypeArguments[1];

                var fieldName = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (fieldName is not null)
                    FieldRefAccessHelper.FindAndReportForFieldRefAccess(ctx, objectType, fieldType, fieldName);
            }
        }
        
        private static void AnalyzeTypeByName(OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation)
        {
            var ctx = new GenericContext(context.Compilation, () => context.Operation.Syntax.GetLocation(), context.ReportDiagnostic);

            if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                var typeName = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (typeName is null) return;

                MemberHelper.FindAndReportForType(ctx, typeName);
            }
        }
    }
}