using BUTR.Harmony.Analyzer.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class TypeOfToStringCSCodeFixProvider : CodeFixProvider
    {
        private const string title = "Convert to string";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.TypeOfToString);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix is not TypeOfExpressionSyntax typeOfExpression)
                return;
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: ct => TypeOfToStringAsync(context.Document, typeOfExpression, ct),
                    equivalenceKey: title),
                context.Diagnostics);
        }
        
        private static async Task<Document> TypeOfToStringAsync(Document document, TypeOfExpressionSyntax nodeToFix, CancellationToken ct)
        {
            if (nodeToFix.Parent is not ArgumentSyntax argument) return document;
            if (argument.Parent is not ArgumentListSyntax argumentList) return document;
            if (argumentList.Arguments.Count < 2) return document;
            if (!document.SupportsSemanticModel) return document;
            
            var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);

            var arguments = argumentList.Arguments;
            var typeName = RoslynHelper.GetString(semanticModel, arguments[0], ct);
            var memberName = RoslynHelper.GetString(semanticModel, arguments[1], ct);
            editor.ReplaceNode(argumentList, argumentList.WithArguments(arguments
                .RemoveAt(0)
                .RemoveAt(0)
                .Insert(0, SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"\"{typeName}:{memberName}\"")))));
            
            return editor.GetChangedDocument();
        }
    }
}