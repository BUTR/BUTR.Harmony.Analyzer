using BUTR.Harmony.Analyzer.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

using System;
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
        private sealed class Rewriter : CSharpSyntaxRewriter
        {
            private readonly SemanticModel _semanticModel;
            public Rewriter(SemanticModel semanticModel)
            {
                _semanticModel = semanticModel;
            }

            public override SyntaxNode VisitArgumentList(ArgumentListSyntax node)
            {
                var argumentList = (ArgumentListSyntax) base.VisitArgumentList(node);
                var arguments = argumentList.Arguments;

                if (arguments[0].Expression is not LambdaExpressionSyntax lambdaExpression) return argumentList;
                if (_semanticModel.GetSymbolInfo(lambdaExpression.Body).Symbol is not { } lambdaBodySymbol) return argumentList;
                var typeName = NameFormatter.ReflectionName(lambdaBodySymbol.ContainingType);
                var memberName = lambdaBodySymbol.MetadataName;

                return argumentList.WithArguments(arguments
                    .RemoveAt(0)
                    .Insert(0, SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"\"{typeName}:{memberName}\""))));
            }

            public override SyntaxNode VisitGenericName(GenericNameSyntax node)
            {
                var identifier = (GenericNameSyntax) base.VisitGenericName(node);
                if (_semanticModel.GetSymbolInfo(identifier).Symbol is { Kind: SymbolKind.Method } symbolInfo)
                {
                    if (!ReplacementTable.TryGetValue(symbolInfo.MetadataName, out var replacement)) return identifier;
                    return SyntaxFactory.IdentifierName(replacement);
                }
                return identifier;
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                var identifier = (IdentifierNameSyntax) base.VisitIdentifierName(node);
                if (_semanticModel.GetTypeInfo(identifier).Type is { } typeInfo)
                {
                    if (!typeInfo.MetadataName.StartsWith("SymbolExtensions", StringComparison.Ordinal)) return identifier;
                    return identifier.WithIdentifier(SyntaxFactory.Identifier("AccessTools"));
                }
                if (_semanticModel.GetSymbolInfo(identifier).Symbol is { Kind: SymbolKind.Method } symbolInfo)
                {
                    if (!ReplacementTable.TryGetValue(symbolInfo.MetadataName, out var replacement)) return identifier;
                    return identifier.WithIdentifier(SyntaxFactory.Identifier(replacement));
                }
                return identifier;
            }
        }

        private const string title = "Convert to string";

        public static readonly IReadOnlyDictionary<string, string> ReplacementTable = new Dictionary<string, string>
        {
            {"GetConstructorInfo", "DeclaredConstructor"},
            {"GetFieldInfo", "DeclaredField"},
            {"GetFieldRefAccess", "FieldRefAccess"},
            {"GetMethodInfo", "DeclaredMethod"},
            {"GetPropertyInfo", "DeclaredProperty"},
            {"GetPropertyGetter", "DeclaredPropertyGetter"},
            {"GetPropertySetter", "DeclaredPropertySetter"},
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

            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);
            var semanticModel = editor.SemanticModel;

            if (semanticModel.GetSymbolInfo(invocationExpression).Symbol is not IMethodSymbol methodSymbol) return document;
            if (!NameFormatter.ReflectionName(methodSymbol.ContainingType).Contains("SymbolExtensions")) return document;

            editor.ReplaceNode(invocationExpression, new Rewriter(editor.SemanticModel).Visit(invocationExpression));

            return editor.GetChangedDocument();
        }
    }
}