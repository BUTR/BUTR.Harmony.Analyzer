using BUTR.Harmony.Analyzer.Utils;

using Microsoft.CodeAnalysis;

using System.Globalization;

namespace BUTR.Harmony.Analyzer
{
    public static class RuleIdentifiers
    {
        public const string MemberDoesntExists = "BHA0001";
        public const string AssemblyNotFound = "BHA0002";
        public const string TypeNotFound = "BHA0003";
        public const string MissingGetter = "BHA0004";
        public const string MissingSetter = "BHA0005";
        public const string WrongType = "BHA0006";

        public static string GetHelpUri(string idenfifier) => 
            string.Format(CultureInfo.InvariantCulture, "https://github.com/BUTR/BUTR.Harmony.Analyzer/blob/master/docs/Rules/{0}.md", idenfifier);

        internal static readonly DiagnosticDescriptor AssemblyRule = new(
            AssemblyNotFound,
            title: "Assembly does not exist for Type",
            messageFormat: "Assembly '{0}' does not exist for Type '{1}'",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(AssemblyNotFound));

        internal static readonly DiagnosticDescriptor TypeRule = new(
            TypeNotFound,
            title: "Type was not found",
            messageFormat: "Type '{0}' was not found",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(TypeNotFound));

        internal static readonly DiagnosticDescriptor MemberRule = new(
            MemberDoesntExists,
            title: "Member does not exist in Type",
            messageFormat: "Member '{0}' does not exist in Type '{1}'",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(MemberDoesntExists));

        internal static readonly DiagnosticDescriptor PropertyGetterRule = new(
            MissingGetter,
            title: "Property does not have a get method",
            messageFormat: "Member '{0}' does not have a get method",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(MissingGetter));

        internal static readonly DiagnosticDescriptor PropertySetterRule = new(
            MissingSetter,
            title: "Property does not have a set method",
            messageFormat: "Member '{0}'does not have a set method",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(MissingSetter));

        internal static readonly DiagnosticDescriptor WrongTypeRule = new(
            WrongType,
            title: "Wrong type!",
            messageFormat: "Wrong type for Type '{0}'! Expected '{1}', actual '{2}'",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(WrongType));

        internal static Diagnostic ReportAssembly(IOperation operation, string assemblyName, string typeName) =>
            DiagnosticUtils.CreateDiagnostic(AssemblyRule, operation, assemblyName, typeName);

        internal static Diagnostic ReportType(IOperation operation, string typeName) =>
            DiagnosticUtils.CreateDiagnostic(TypeRule, operation, typeName);

        internal static Diagnostic ReportMember(IOperation operation, string typeName, string memberName) =>
            DiagnosticUtils.CreateDiagnostic(MemberRule, operation, memberName, typeName);

        internal static Diagnostic ReportMissingGetter(IOperation operation, string propertyName) =>
            DiagnosticUtils.CreateDiagnostic(PropertyGetterRule, operation, propertyName);

        internal static Diagnostic ReportMissingSetter(IOperation operation, string propertyName) =>
            DiagnosticUtils.CreateDiagnostic(PropertySetterRule, operation, propertyName);

        internal static Diagnostic ReportWrongType(IOperation operation, string holderType, string expectedType, string actualType) =>
            DiagnosticUtils.CreateDiagnostic(WrongTypeRule, operation, holderType, expectedType, actualType);
    }
}