using BUTR.Harmony.Analyzer.Data;
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
        public const string MissingConstructor = "BHA0007";
        public const string MissingStaticConstructor = "BHA0008";
        public const string NotInstanceField = "BHA0009";
        public const string NotStaticField = "BHA0010";

        private static string GetHelpUri(string idenfifier) =>
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
            messageFormat: "Property '{0}' does not have a get method",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(MissingGetter));

        internal static readonly DiagnosticDescriptor PropertySetterRule = new(
            MissingSetter,
            title: "Property does not have a set method",
            messageFormat: "Property '{0}'does not have a set method",
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


        internal static readonly DiagnosticDescriptor ConstructorRule = new(
            MissingConstructor,
            title: "Type does not have the constructor",
            messageFormat: "Type '{0}' does not have the constructor",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(MissingConstructor));


        internal static readonly DiagnosticDescriptor StaticConstructorRule = new(
            MissingStaticConstructor,
            title: "Type does not have the static constructor",
            messageFormat: "Type '{0}' does not have the static constructor",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(MissingStaticConstructor));
        
        internal static readonly DiagnosticDescriptor NotInstanceFieldRule = new(
            MissingStaticConstructor,
            title: "Field is static",
            messageFormat: "Field '{0}' is static",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(NotInstanceField));
        
        internal static readonly DiagnosticDescriptor NotStaticFieldRule = new(
            MissingStaticConstructor,
            title: "Field is not static",
            messageFormat: "Field '{0}' is not static",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: GetHelpUri(NotStaticField));

        internal static Diagnostic ReportAssembly(GenericContext context, string assemblyName, string typeName) =>
            DiagnosticUtils.CreateDiagnostic(AssemblyRule, context, assemblyName, typeName);

        internal static Diagnostic ReportType(GenericContext context, string typeName) =>
            DiagnosticUtils.CreateDiagnostic(TypeRule, context, typeName);

        internal static Diagnostic ReportMember(GenericContext context, string typeName, string memberName) =>
            DiagnosticUtils.CreateDiagnostic(MemberRule, context, memberName, typeName);

        internal static Diagnostic ReportMissingGetter(GenericContext context, string propertyName) =>
            DiagnosticUtils.CreateDiagnostic(PropertyGetterRule, context, propertyName);

        internal static Diagnostic ReportMissingSetter(GenericContext context, string propertyName) =>
            DiagnosticUtils.CreateDiagnostic(PropertySetterRule, context, propertyName);

        internal static Diagnostic ReportWrongType(GenericContext context, string holderType, string expectedType, string actualType) =>
            DiagnosticUtils.CreateDiagnostic(WrongTypeRule, context, holderType, expectedType, actualType);

        internal static Diagnostic ReportMissingConstructor(GenericContext context, string typeName) =>
            DiagnosticUtils.CreateDiagnostic(ConstructorRule, context, typeName);

        internal static Diagnostic ReportMissingStaticConstructor(GenericContext context, string typeName) =>
            DiagnosticUtils.CreateDiagnostic(StaticConstructorRule, context, typeName);
        
        internal static Diagnostic ReportNotInstanceField(GenericContext context, string fieldName) =>
            DiagnosticUtils.CreateDiagnostic(NotInstanceFieldRule, context, fieldName);
        
        internal static Diagnostic ReportNotStaticField(GenericContext context, string fieldName) =>
            DiagnosticUtils.CreateDiagnostic(NotStaticFieldRule, context, fieldName);
    }
}