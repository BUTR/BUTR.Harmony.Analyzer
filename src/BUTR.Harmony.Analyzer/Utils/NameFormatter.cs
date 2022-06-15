using BUTR.Harmony.Analyzer.Data;

using Microsoft.CodeAnalysis;

namespace BUTR.Harmony.Analyzer.Utils
{
    public static class NameFormatter
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

        public static string ReflectionName(ISymbol typeSymbol) => typeSymbol.ContainingNamespace is not null
            ? $"{typeSymbol.ContainingNamespace}.{typeSymbol.MetadataName}"
            : string.IsNullOrEmpty(typeSymbol.MetadataName)
                ? typeSymbol.ToDisplayString(Style) // TODO: Bad fallback
                : typeSymbol.MetadataName;

        internal static string ReflectionName(SignatureType signatureType) => signatureType.ToString(true) ?? string.Empty;
        internal static string ReflectionGenericName(SignatureType signatureType) => signatureType.Name;
    }
}