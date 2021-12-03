using Microsoft.CodeAnalysis;

using System.Collections.Immutable;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal static class DiagnosticUtils
    {
        private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, Location location, ImmutableDictionary<string, string?>? properties, params string[] messageArgs)
        {
            return Diagnostic.Create(descriptor, location, properties, messageArgs);
        }

        public static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, IOperation operation, params string[] messageArgs)
        {
            return CreateDiagnostic(descriptor, ImmutableDictionary<string, string?>.Empty, operation, messageArgs);
        }
        public static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, ImmutableDictionary<string, string?>? properties, IOperation operation, params string[] messageArgs)
        {
            return CreateDiagnostic(descriptor, operation.Syntax.GetLocation(), properties, messageArgs);
        }
    }
}