using BUTR.Harmony.Analyzer.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class SymbolExtensionsToStringCSCodeFixProvider : CodeFixProvider
    {
        private const string title = "Convert to string";

        public static readonly IReadOnlyDictionary<string, string> ReplacementTable = new Dictionary<string, string>
        {
            {"GetConstructorInfo", "DeclaredConstructor"},
            {"GetFieldInfo", "DeclaredField"},
            {"GetFieldRefAccess", "FieldRefAccess"},
            {"GetMethodInfo", "DeclaredMethod"},
            {"GetPropertyInfo", "DeclaredProperty"},
            {"GetStaticFieldRefAccess", "StaticFieldRefAccess"},
            {"GetStructFieldRefAccess", "StructFieldRefAccess"},
        };

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.SymbolExtensionsToString);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix is not LambdaExpressionSyntax lambdaExpressionSyntax)
                return;
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: ct => TypeOfToStringAsync(context.Document, lambdaExpressionSyntax, ct),
                    equivalenceKey: title),
                context.Diagnostics);
        }
        
        private static async Task<Document> TypeOfToStringAsync(Document document, LambdaExpressionSyntax nodeToFix, CancellationToken ct)
        {
            if (nodeToFix.Parent is not ArgumentSyntax argument) return document;
            if (argument.Parent is not ArgumentListSyntax argumentList) return document;
            if (argumentList.Parent is not InvocationExpressionSyntax invocationExpression) return document;
            if (argumentList.Arguments.Count != 1) return document;
            if (!document.SupportsSemanticModel) return document;

            return nodeToFix.Body switch
            {
                MemberAccessExpressionSyntax => await ReplaceFieldProperty(document, nodeToFix, invocationExpression, argumentList, ct).ConfigureAwait(false),
                InvocationExpressionSyntax => await ReplaceMethod(document, nodeToFix, invocationExpression, argumentList, ct).ConfigureAwait(false),
                _ => document
            };
        }

        private static async Task<Document> ReplaceFieldProperty(Document document, LambdaExpressionSyntax nodeToFix, InvocationExpressionSyntax invocation, ArgumentListSyntax argumentList, CancellationToken ct)
        {
            var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);

            var arguments = argumentList.Arguments;
            if (nodeToFix.Body is not MemberAccessExpressionSyntax memberAccessExpression) return document;
            if (memberAccessExpression.Expression is not IdentifierNameSyntax typeIdentifierName) return document;
            if (memberAccessExpression.Name is not IdentifierNameSyntax fieldIdentifierName) return document;
            var typeName = NameFormatter.ReflectionName(semanticModel.GetTypeInfo(typeIdentifierName).Type);
            var memberName = semanticModel.GetSymbolInfo(fieldIdentifierName).Symbol.MetadataName;
            arguments = arguments.RemoveAt(0).Insert(0, SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"\"{typeName}:{memberName}\"")));
            editor.ReplaceNode(argumentList, argumentList.WithArguments(arguments));

            if (invocation.Expression is not MemberAccessExpressionSyntax methodExpression) return document;
            if (methodExpression.Expression is not MemberAccessExpressionSyntax typeExpression) return document;
            var oldMemberName = methodExpression.Name.ToString().IndexOf('<') is var idx and not -1
                ? methodExpression.Name.ToString().Substring(0, idx)
                : methodExpression.Name.ToString();
            var newMemberName = SyntaxFactory.IdentifierName(ReplacementTable.TryGetValue(oldMemberName, out var var)
                ? var
                : methodExpression.Name.ToString());
            var newTypeName = SyntaxFactory.IdentifierName(typeExpression.Name.ToString().Replace("SymbolExtensions", "AccessTools"));
            editor.ReplaceNode(methodExpression, methodExpression.WithName(newMemberName).WithExpression(typeExpression.WithName(newTypeName)));

            return editor.GetChangedDocument();
        }
        
        private static async Task<Document> ReplaceMethod(Document document, LambdaExpressionSyntax nodeToFix, InvocationExpressionSyntax invocation, ArgumentListSyntax argumentList, CancellationToken ct)
        {
            var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);

            var arguments = argumentList.Arguments;
            if (nodeToFix.Body is not InvocationExpressionSyntax lambdaInvocationExpression) return document;
            if (lambdaInvocationExpression.Expression is not MemberAccessExpressionSyntax methodLambdaExpression) return document;
            if (methodLambdaExpression.Expression is not MemberAccessExpressionSyntax typeLambdaExpression) return document;
            var typeName = NameFormatter.ReflectionName(semanticModel.GetTypeInfo(typeLambdaExpression).Type);
            var memberName = semanticModel.GetSymbolInfo(methodLambdaExpression).Symbol.MetadataName;
            arguments = arguments.RemoveAt(0).Insert(0, SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"\"{typeName}:{memberName}\"")));
            editor.ReplaceNode(argumentList, argumentList.WithArguments(arguments));

            if (invocation.Expression is not MemberAccessExpressionSyntax methodExpression) return document;
            if (methodExpression.Expression is not MemberAccessExpressionSyntax typeExpression) return document;
            var oldMethodName = methodExpression.Name.ToString().IndexOf('<') is var idx and not -1
                ? methodExpression.Name.ToString().Substring(0, idx)
                : methodExpression.Name.ToString();
            var newMethodName = SyntaxFactory.IdentifierName(ReplacementTable.TryGetValue(oldMethodName, out var var)
                ? var
                : methodExpression.Name.ToString());
            var newTypeName = SyntaxFactory.IdentifierName(typeExpression.Name.ToString().Replace("SymbolExtensions", "AccessTools"));
            editor.ReplaceNode(methodExpression, methodExpression.WithName(newMethodName).WithExpression(typeExpression.WithName(newTypeName)));

            return editor.GetChangedDocument();
        }
    }
}