using BUTR.Harmony.Analyzer.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BUTR.Harmony.Analyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    public class TypeOfToStringFullNameCSCodeFixProvider : CodeFixProvider
    {
        private const string title = "Convert to string";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(RuleIdentifiers.TypeOfToStringFullName);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var nodeToFix = root?.FindNode(context.Span, getInnermostNodeForTie: true);
            if (nodeToFix is not InvocationExpressionSyntax invocationExpressionSyntax)
                return;
            
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: ct => TypeOfToStringAsync(context.Document, invocationExpressionSyntax, ct),
                    equivalenceKey: title),
                context.Diagnostics.First());
        }
        
        private async Task<Document> TypeOfToStringAsync(Document document, InvocationExpressionSyntax nodeToFix, CancellationToken ct)
        {
            var semanticModel = await document.GetSemanticModelAsync(ct).ConfigureAwait(false);
            
            var editor = await DocumentEditor.CreateAsync(document, ct).ConfigureAwait(false);

            if (nodeToFix.ArgumentList.Arguments.Count < 2)
                return document;
            
            var arguments = nodeToFix.ArgumentList.Arguments;
            
            var typeName = RoslynHelper.GetString(semanticModel, arguments[0], ct);
            var memberName = RoslynHelper.GetString(semanticModel, arguments[1], ct);

            arguments = arguments.RemoveAt(0);
            arguments = arguments.RemoveAt(0);
            arguments = arguments.Insert(0, SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"\"{typeName}:{memberName}\"")));

            editor.ReplaceNode(nodeToFix, nodeToFix.WithArgumentList(nodeToFix.ArgumentList.WithArguments(arguments)));
            return editor.GetChangedDocument();
        }
    }
}