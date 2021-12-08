using Microsoft.CodeAnalysis;

using System;

namespace BUTR.Harmony.Analyzer.Data
{
    public class GenericContext
    {
        private readonly Action<Diagnostic> _reportDiagnostic;
        private readonly Func<Location> _getLocation;

        public Compilation Compilation { get; }

        public GenericContext(Compilation compilation, Func<Location> getLocation, Action<Diagnostic> reportDiagnostic)
        {
            Compilation = compilation;
            _getLocation = getLocation;
            _reportDiagnostic = reportDiagnostic;
        }

        public Location GetLocation() => _getLocation();
        public void ReportDiagnostic(Diagnostic diagnostic) => _reportDiagnostic(diagnostic);
    }
}