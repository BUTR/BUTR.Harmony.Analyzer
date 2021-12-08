using Microsoft.CodeAnalysis;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class NameFormatter
    {
        private static readonly SymbolDisplayFormat Style = new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            kindOptions: SymbolDisplayKindOptions.None,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None,
            memberOptions: SymbolDisplayMemberOptions.None,
            localOptions: SymbolDisplayLocalOptions.None,
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            extensionMethodStyle: SymbolDisplayExtensionMethodStyle.Default,
            parameterOptions: SymbolDisplayParameterOptions.None
        );

        public static string ReflectionName(ITypeSymbol typeSymbol) => typeSymbol.ContainingNamespace is not null
            ? $"{typeSymbol.ContainingNamespace}.{typeSymbol.MetadataName}"
            : string.IsNullOrEmpty(typeSymbol.MetadataName)
                ? typeSymbol.ToDisplayString(Style) // TODO:
                : typeSymbol.MetadataName;
    }
}