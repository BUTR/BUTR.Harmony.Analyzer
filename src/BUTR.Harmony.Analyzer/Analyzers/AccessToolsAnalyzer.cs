﻿using BUTR.Harmony.Analyzer.Data;
using BUTR.Harmony.Analyzer.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace BUTR.Harmony.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AccessToolsAnalyzer : DiagnosticAnalyzer
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
                diagnostics[invocationOperation] = AnalyzeInvocationAndReport(context, invocationOperation);
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
            
            foreach (var diagnotic in AnalyzeInvocationAndReport(context, invocationOperation))
            {
                context.ReportDiagnostic(diagnotic);
            }
        }
        
        private static ImmutableArray<Diagnostic> AnalyzeInvocationAndReport(OperationAnalysisContext context, IInvocationOperation invocationOperation)
        {
            if (invocationOperation.Syntax is not InvocationExpressionSyntax invocation) return ImmutableArray<Diagnostic>.Empty;

            if (!invocationOperation.TargetMethod.ContainingType.Name.StartsWith("AccessTools", StringComparison.Ordinal)) return ImmutableArray<Diagnostic>.Empty;

            var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
            var ctx = new GenericContext(context.Compilation, () => invocationOperation.Syntax.GetLocation(), diagnostic => diagnostics.Add(diagnostic));

            var methodName = invocationOperation.TargetMethod.Name.AsSpan();

            var flags = MemberFlags.None;

            if (methodName.Equals("FieldRefAccess".AsSpan(), StringComparison.Ordinal))
            {
                AnalyzeFieldRefAccess(ctx, context, invocationOperation, invocation);
                return diagnostics.ToImmutable();
            }
            else if (methodName.Equals("StructFieldRefAccess".AsSpan(), StringComparison.Ordinal))
            {
                AnalyzeStructFieldRefAccess(ctx, context, invocationOperation, invocation);
                return diagnostics.ToImmutable();
            }
            else if (methodName.Equals("StaticFieldRefAccess".AsSpan(), StringComparison.Ordinal))
            {
                AnalyzeStaticFieldRefAccess(ctx, context, invocationOperation, invocation);
                return diagnostics.ToImmutable();
            }
            else if (methodName.Equals("TypeByName".AsSpan(), StringComparison.Ordinal))
            {
                AnalyzeTypeByName(ctx, context, invocationOperation, invocation);
                return diagnostics.ToImmutable();
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
                AnalyzeConstructor(ctx, context, invocationOperation, invocation, flags);
                return diagnostics.ToImmutable();
            }

            AnalyzeMembers(ctx, context, invocationOperation, invocation, flags);
            return diagnostics.ToImmutable();
        }

        private static void AnalyzeConstructor(GenericContext ctx, OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation, MemberFlags memberFlags)
        {
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
        
        private static void AnalyzeMembers(GenericContext ctx, OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation, MemberFlags memberFlags)
        {
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

        private static void AnalyzeFieldRefAccess(GenericContext ctx, OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation)
        {
            if (operation.TargetMethod.Arity == 1)
            {
                if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
                {
                    var fieldType = operation.TargetMethod.TypeArguments[0];
                    
                    var typeSemicolonMember = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                    if (typeSemicolonMember is null) return;

                    FieldRefAccessHelper.FindAndReportForFieldRefAccess(ctx, fieldType, typeSemicolonMember, false);
                }
            }
            
            if (operation.TargetMethod.Arity == 2)
            {
                if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
                {
                    var objectType = operation.TargetMethod.TypeArguments[0];
                    var fieldType = operation.TargetMethod.TypeArguments[1];

                    var fieldName = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                    if (fieldName is not null)
                        FieldRefAccessHelper.FindAndReportForFieldRefAccess(ctx, fieldType, $"{NameFormatter.ReflectionName(objectType)}:{fieldName}", false);
                }
            }
        }
        
        private static void AnalyzeStaticFieldRefAccess(GenericContext ctx, OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation)
        {
            if (operation.TargetMethod.Arity == 1)
            {
                if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
                {
                    var fieldType = operation.TargetMethod.TypeArguments[0];
                    
                    var typeSemicolonMember = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                    if (typeSemicolonMember is null) return;

                    FieldRefAccessHelper.FindAndReportForFieldRefAccess(ctx, fieldType, typeSemicolonMember, true);
                }
            }
            
            if (operation.TargetMethod.Arity == 2)
            {
                if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
                {
                    var objectType = operation.TargetMethod.TypeArguments[0];
                    var fieldType = operation.TargetMethod.TypeArguments[1];

                    var fieldName = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                    if (fieldName is not null)
                        FieldRefAccessHelper.FindAndReportForFieldRefAccess(ctx, fieldType, $"{NameFormatter.ReflectionName(objectType)}:{fieldName}", true);
                }
            }
        }
        
        private static void AnalyzeStructFieldRefAccess(GenericContext ctx, OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation)
        {
            if (operation.TargetMethod.Arity == 1)
            {
                if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
                {
                    var fieldType = operation.TargetMethod.TypeArguments[0];
                    
                    var typeSemicolonMember = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                    if (typeSemicolonMember is null) return;

                    FieldRefAccessHelper.FindAndReportForFieldRefAccess(ctx, fieldType, typeSemicolonMember, false);
                }
            }
            
            if (operation.TargetMethod.Arity == 2)
            {
                if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
                {
                    var objectType = operation.TargetMethod.TypeArguments[0];
                    var fieldType = operation.TargetMethod.TypeArguments[1];

                    var fieldName = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                    if (fieldName is not null)
                        FieldRefAccessHelper.FindAndReportForFieldRefAccess(ctx, fieldType, $"{NameFormatter.ReflectionName(objectType)}:{fieldName}", false);
                }
            }
        }
        
        private static void AnalyzeTypeByName(GenericContext ctx, OperationAnalysisContext context, IInvocationOperation operation, InvocationExpressionSyntax invocation)
        {
            if (operation.TargetMethod.Parameters.Length == 1 && string.Equals(operation.TargetMethod.Parameters[0].Type.Name, nameof(String), StringComparison.OrdinalIgnoreCase))
            {
                var typeName = RoslynHelper.GetString(operation.SemanticModel, invocation.ArgumentList.Arguments[0], context.CancellationToken);
                if (typeName is null) return;

                MemberHelper.FindAndReportForType(ctx, typeName);
            }
        }
    }
}