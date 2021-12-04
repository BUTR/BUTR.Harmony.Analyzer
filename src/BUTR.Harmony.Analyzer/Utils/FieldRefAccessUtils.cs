using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace BUTR.Harmony.Analyzer.Utils
{
    internal class FieldRefAccessUtils
    {
        public static readonly DiagnosticDescriptor WrongTypeRule = new(
            RuleIdentifiers.WrongType,
            title: "Wrong type!",
            messageFormat: "Wrong type for Type '{0}'! Expected '{1}', actual '{2}'",
            RuleCategories.Usage,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "",
            helpLinkUri: RuleIdentifiers.GetHelpUri(RuleIdentifiers.WrongType));

        private static Diagnostic ReportAssembly(IOperation operation, string assemblyName, string typeName)
        {
            return DiagnosticUtils.CreateDiagnostic(MemberUtils.AssemblyRule, operation, assemblyName, typeName);
        }
        private static Diagnostic ReportType(IOperation operation, string typeName)
        {
            return DiagnosticUtils.CreateDiagnostic(MemberUtils.TypeRule, operation, typeName);
        }
        private static Diagnostic ReportMember(IOperation operation, string typeName, string memberName)
        {
            return DiagnosticUtils.CreateDiagnostic(MemberUtils.MemberRule, operation, memberName, typeName);
        }
        private static Diagnostic ReportWrongType(IOperation operation, string holderType, string expectedType, string actualType)
        {
            return DiagnosticUtils.CreateDiagnostic(WrongTypeRule, operation, holderType, expectedType, actualType);
        }


        public static void FindAndReportForFieldRefAccess(OperationAnalysisContext context, ITypeSymbol objectType, ITypeSymbol fieldType, string fieldName)
        {
            foreach (var diagnostic in DiagnosticsFieldRefAccess(context, objectType, fieldType, fieldName))
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<Diagnostic> DiagnosticsFieldRefAccess(OperationAnalysisContext context, ITypeSymbol objectType, ITypeSymbol fieldType, string fieldName)
        {
            var obectTypeRef = context.Compilation.GetMetadataReference(objectType.ContainingAssembly);
            //var fieldTypeRef = context.Compilation.GetMetadataReference(fieldType.ContainingAssembly);

            if (obectTypeRef is PortableExecutableReference otr && File.Exists(otr.FilePath)/* && fieldTypeRef is PortableExecutableReference ftr && File.Exists(ftr.FilePath)*/)
            {
                return FindInFileMetadata(context, otr.FilePath, objectType, fieldType, fieldName);
            }
            else // Fallback to roslyn based finding
            {
                return FindInRoslynMetadata(context, objectType, fieldType, fieldName);
            }
        }

        private static IEnumerable<Diagnostic> FindInFileMetadata(OperationAnalysisContext context, string objectTypeAsmFilePath, ITypeSymbol objectType, ITypeSymbol fieldType, string fieldName)
        {
            using var peReaderObject = new PEReader(File.ReadAllBytes(objectTypeAsmFilePath).ToImmutableArray());
            var metadataObject = peReaderObject.GetMetadataReader();

            var objectTypeName = objectType.ToDisplayString(ReflectionUtils.Style);
            var fieldTypeName = fieldType.ToDisplayString(ReflectionUtils.Style);


            if (ReflectionUtils.FindTypeDefinition(metadataObject, objectTypeName) is not { } objectTypeDefinition)
            {
                yield return ReportType(context.Operation, objectTypeName);
                yield break;
            }

            //if (ReflectionUtils.FindTypeReference(metadataObject, fieldTypeName) is not { } findTypeReference)
            //{
            //    yield return ReportType(operation, fieldTypeName);
            //    yield break;
            //}

            if (ReflectionUtils.FindFieldDefinition(context, metadataObject, objectTypeDefinition, false, fieldName) is not { } fieldDefinition)
            {
                yield return ReportMember(context.Operation, objectTypeName, fieldName);
                yield break;
            }

            var ctx = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            var fieldDefinitionType = fieldDefinition.DecodeSignature(new DisassemblingTypeProvider(), ctx);
            var fieldDefinitionTypeWithoutArity = RemoveArity(fieldDefinitionType);

            if (!fieldTypeName.Equals(fieldDefinitionTypeWithoutArity, StringComparison.Ordinal))
            {
                yield return ReportWrongType(context.Operation, objectTypeName, fieldTypeName, fieldDefinitionType);
                yield break;
            }
        }

        private static string RemoveArity(string str)
        {
            while (str.Contains("`"))
            {
                str = RemoveArityInternal(str.AsSpan());
            }
            return str;
        }

        private static string RemoveArityInternal(ReadOnlySpan<char> str)
        {
            var arityStart = str.IndexOf('`');
            var arityEnd = str.IndexOf('<');
            if (arityStart > arityEnd)
            {
                throw new Exception();
            }

            var pt1 = str.Slice(0, arityStart);
            var pt2 = str.Slice(arityEnd);

            var buffer = new char[pt1.Length + pt2.Length];
            var span = new Span<char>(buffer);

            pt1.CopyTo(span);
            pt2.CopyTo(span.Slice(pt1.Length));

            return new string(buffer);
        }

        private static IEnumerable<Diagnostic> FindInRoslynMetadata(OperationAnalysisContext context, ITypeSymbol objectType, ITypeSymbol fieldType, string fieldName)
        {
            var objectTypeName = objectType.ToDisplayString(ReflectionUtils.Style);

            //var fieldType = fieldTypeAssembly.GetTypeByMetadataName(fieldTypeName);
            //if (fieldType is null)
            //{
            //    yield return ReportType(operation, fieldTypeName);
            //    yield break;
            //}

            while (true)
            {
                var foundMembers = objectType.GetMembers(fieldName);
                foreach (var member in foundMembers)
                {
                    if (member is not IFieldSymbol)
                    {
                        yield return ReportMember(context.Operation, objectTypeName, fieldName);
                    }
                    yield break;
                }

                if (false && objectType.BaseType is { } baseType)
                {
                    objectType = baseType;
                    continue;
                }

                break;
            }

            // We haven't found the member in the exact type or base classes. Report that.
            yield return ReportMember(context.Operation, objectTypeName, fieldName);
        }
    }
}