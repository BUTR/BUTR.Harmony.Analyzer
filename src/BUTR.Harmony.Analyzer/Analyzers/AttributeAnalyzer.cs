using BUTR.Harmony.Analyzer.Data;
using BUTR.Harmony.Analyzer.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using System;
using System.Collections.Immutable;
using System.Linq;

namespace BUTR.Harmony.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AttributeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            RuleIdentifiers.MemberRule,
            RuleIdentifiers.AssemblyRule,
            RuleIdentifiers.TypeRule,
            RuleIdentifiers.PropertyGetterRule,
            RuleIdentifiers.PropertySetterRule,
            RuleIdentifiers.WrongTypeRule
        );

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSymbolAction(Action, SymbolKind.NamedType);
        }

        private static void Action(SymbolAnalysisContext context)
        {
            if (context.Symbol is not INamedTypeSymbol namedTypeSymbol) return;

            var tt = namedTypeSymbol.GetAttributes();
            var attributes = namedTypeSymbol.GetAttributes()
                .Where(a =>
                    string.Equals(a.AttributeClass.ContainingNamespace.Name, "HarmonyLib", StringComparison.Ordinal) &&
                    a.AttributeClass.Name.StartsWith("HarmonyPatch", StringComparison.Ordinal))
                .ToImmutableArray();

            if (attributes.Length == 0) return;

            var attr = attributes.First();
            var ctx = new GenericContext(
                context.Compilation,
                () => attr.ApplicationSyntaxReference.SyntaxTree.GetLocation(attr.ApplicationSyntaxReference.Span),
                context.ReportDiagnostic);

            var args = attributes.SelectMany(x => x.ConstructorArguments).ToImmutableArray();
            var typeOfArg = args.FirstOrDefault(x => x.Kind == TypedConstantKind.Type && x.Value is INamedTypeSymbol);
            var methodArg = args.FirstOrDefault(x => x.Kind == TypedConstantKind.Primitive && x.Value is string);
            var methodTypeArg = args.FirstOrDefault(x => x.Kind == TypedConstantKind.Enum);
            var paramsTypeArg = args.FirstOrDefault(x => x.Kind == TypedConstantKind.Array && x.Values.Any(y => y.Kind == TypedConstantKind.Type));
            var paramsVariationArg = args.FirstOrDefault(x => x.Kind == TypedConstantKind.Array && x.Values.Any(y => y.Kind == TypedConstantKind.Enum));

            if (!typeOfArg.IsNull)
            {
                MemberHelper.FindAndReportForAnyTypeMember(
                    ctx,
                    (INamedTypeSymbol) typeOfArg.Value,
                    (string) methodArg.Value,
                    methodTypeArg.IsNull ? MethodType.Normal : (MethodType) methodTypeArg.Value,
                    paramsTypeArg.IsNull ? null : paramsTypeArg.Values.Select(x => x.Value).OfType<ITypeSymbol>().ToImmutableArray(),
                    paramsVariationArg.IsNull ? null : paramsVariationArg.Values.Select(x => x.Value).OfType<int>().Cast<ArgumentType>().ToImmutableArray());
            }
        }
    }
}