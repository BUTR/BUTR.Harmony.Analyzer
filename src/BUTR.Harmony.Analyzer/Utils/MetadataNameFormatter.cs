using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace BUTR.Harmony.Analyzer.Utils
{
    /// <summary>
    /// Helper class for converting metadata tokens into their textual representation.
    /// </summary>
    public class MetadataNameFormatter : DisassemblingTypeProvider
    {
        private readonly MetadataReader _metadataReader;

        public MetadataNameFormatter(MetadataReader metadataReader)
        {
            _metadataReader = metadataReader;
        }

        public static string FormatHandle(MetadataReader metadataReader, Handle handle, bool namespaceQualified = true, string? owningTypeOverride = null, string signaturePrefix = "")
        {
            var formatter = new MetadataNameFormatter(metadataReader);
            return formatter.EmitHandleName(handle, namespaceQualified, owningTypeOverride, signaturePrefix);
        }

        private string EmitHandleName(Handle handle, bool namespaceQualified, string? owningTypeOverride, string signaturePrefix = "")
        {
            try
            {
                switch (handle.Kind)
                {
                    case HandleKind.MemberReference:
                        return EmitMemberReferenceName((MemberReferenceHandle) handle, owningTypeOverride, signaturePrefix);

                    case HandleKind.MethodSpecification:
                        return EmitMethodSpecificationName((MethodSpecificationHandle) handle, owningTypeOverride, signaturePrefix);

                    case HandleKind.MethodDefinition:
                        return EmitMethodDefinitionName((MethodDefinitionHandle) handle, owningTypeOverride, signaturePrefix);

                    case HandleKind.TypeReference:
                        return EmitTypeReferenceName((TypeReferenceHandle) handle, namespaceQualified, signaturePrefix);

                    case HandleKind.TypeSpecification:
                        return EmitTypeSpecificationName((TypeSpecificationHandle) handle, namespaceQualified, signaturePrefix);

                    case HandleKind.TypeDefinition:
                        return EmitTypeDefinitionName((TypeDefinitionHandle) handle, namespaceQualified, signaturePrefix);

                    case HandleKind.FieldDefinition:
                        return EmitFieldDefinitionName((FieldDefinitionHandle) handle, namespaceQualified, owningTypeOverride, signaturePrefix);

                    default:
                        throw new NotImplementedException();
                }
            }
            catch (Exception ex)
            {
                return $"$$INVALID-{handle.Kind}-{MetadataTokens.GetRowNumber((EntityHandle) handle):X6}: {ex.Message}";
            }
        }

        private void ValidateHandle(EntityHandle handle, TableIndex tableIndex)
        {
            var rowid = MetadataTokens.GetRowNumber(handle);
            var tableRowCount = _metadataReader.GetTableRowCount(tableIndex);
            if (rowid <= 0 || rowid > tableRowCount)
            {
                throw new NotImplementedException($"Invalid handle {MetadataTokens.GetToken(handle):X8} in table {tableIndex.ToString()} ({tableRowCount} rows)");
            }
        }

        private string EmitMethodSpecificationName(MethodSpecificationHandle methodSpecHandle, string? owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(methodSpecHandle, TableIndex.MethodSpec);
            var methodSpec = _metadataReader.GetMethodSpecification(methodSpecHandle);
            var genericContext = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            return EmitHandleName(methodSpec.Method, namespaceQualified: true, owningTypeOverride: owningTypeOverride, signaturePrefix: signaturePrefix)
                + methodSpec.DecodeSignature(this, genericContext);
        }

        private string EmitMemberReferenceName(MemberReferenceHandle memberRefHandle, string? owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(memberRefHandle, TableIndex.MemberRef);
            var memberRef = _metadataReader.GetMemberReference(memberRefHandle);
            var builder = new StringBuilder();
            var genericContext = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            switch (memberRef.GetKind())
            {
                case MemberReferenceKind.Field:
                {
                    var fieldSig = memberRef.DecodeFieldSignature(this, genericContext);
                    builder.Append(fieldSig);
                    builder.Append(" ");
                    builder.Append(EmitContainingTypeAndMemberName(memberRef, owningTypeOverride, signaturePrefix));
                    break;
                }

                case MemberReferenceKind.Method:
                {
                    var methodSig = memberRef.DecodeMethodSignature(this, genericContext);
                    builder.Append(methodSig.ReturnType);
                    builder.Append(" ");
                    builder.Append(EmitContainingTypeAndMemberName(memberRef, owningTypeOverride, signaturePrefix));
                    builder.Append(EmitMethodSignature(methodSig));
                    break;
                }

                default:
                    throw new NotImplementedException(memberRef.GetKind().ToString());
            }

            return builder.ToString();
        }

        private string EmitMethodDefinitionName(MethodDefinitionHandle methodDefinitionHandle, string? owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(methodDefinitionHandle, TableIndex.MethodDef);
            var methodDef = _metadataReader.GetMethodDefinition(methodDefinitionHandle);
            var genericContext = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            var methodSig = methodDef.DecodeSignature(this, genericContext);
            var builder = new StringBuilder();
            builder.Append(methodSig.ReturnType);
            builder.Append(" ");
            if (owningTypeOverride == null)
            {
                owningTypeOverride = EmitHandleName(methodDef.GetDeclaringType(), namespaceQualified: true, owningTypeOverride: null);
            }
            builder.Append(owningTypeOverride);
            builder.Append(".");
            builder.Append(signaturePrefix);
            builder.Append(EmitString(methodDef.Name));
            builder.Append(EmitMethodSignature(methodSig));
            return builder.ToString();
        }

        private string EmitMethodSignature(MethodSignature<string> methodSignature)
        {
            var builder = new StringBuilder();
            if (methodSignature.GenericParameterCount != 0)
            {
                builder.Append("<");
                var firstTypeArg = true;
                for (var typeArgIndex = 0; typeArgIndex < methodSignature.GenericParameterCount; typeArgIndex++)
                {
                    if (firstTypeArg)
                    {
                        firstTypeArg = false;
                    }
                    else
                    {
                        builder.Append(", ");
                    }
                    builder.Append("!!");
                    builder.Append(typeArgIndex);
                }
                builder.Append(">");
            }
            builder.Append("(");
            var firstMethodArg = true;
            foreach (var paramType in methodSignature.ParameterTypes)
            {
                if (firstMethodArg)
                {
                    firstMethodArg = false;
                }
                else
                {
                    builder.Append(", ");
                }
                builder.Append(paramType);
            }
            builder.Append(")");
            return builder.ToString();
        }

        private string EmitContainingTypeAndMemberName(MemberReference memberRef, string? owningTypeOverride, string signaturePrefix)
        {
            if (owningTypeOverride == null)
            {
                owningTypeOverride = EmitHandleName(memberRef.Parent, namespaceQualified: true, owningTypeOverride: null);
            }
            return owningTypeOverride + "." + signaturePrefix + EmitString(memberRef.Name);
        }

        private string EmitTypeReferenceName(TypeReferenceHandle typeRefHandle, bool namespaceQualified, string signaturePrefix)
        {
            ValidateHandle(typeRefHandle, TableIndex.TypeRef);
            var typeRef = _metadataReader.GetTypeReference(typeRefHandle);
            var typeName = EmitString(typeRef.Name);
            var output = "";
            if (typeRef.ResolutionScope.Kind != HandleKind.AssemblyReference)
            {
                // Nested type - format enclosing type followed by the nested type
                return EmitHandleName(typeRef.ResolutionScope, namespaceQualified, owningTypeOverride: null) + "+" + typeName;
            }
            if (namespaceQualified)
            {
                output = EmitString(typeRef.Namespace);
                if (!string.IsNullOrEmpty(output))
                {
                    output += ".";
                }
            }
            return output + signaturePrefix + typeName;
        }

        private string EmitTypeDefinitionName(TypeDefinitionHandle typeDefHandle, bool namespaceQualified, string signaturePrefix)
        {
            ValidateHandle(typeDefHandle, TableIndex.TypeDef);
            var typeDef = _metadataReader.GetTypeDefinition(typeDefHandle);
            var typeName = signaturePrefix + EmitString(typeDef.Name);
            if (typeDef.IsNested)
            {
                // Nested type
                return EmitHandleName(typeDef.GetDeclaringType(), namespaceQualified, owningTypeOverride: null) + "+" + typeName;
            }

            string output;
            if (namespaceQualified)
            {
                output = EmitString(typeDef.Namespace);
                if (!string.IsNullOrEmpty(output))
                {
                    output += ".";
                }
            }
            else
            {
                output = "";
            }
            return output + typeName;
        }

        private string EmitTypeSpecificationName(TypeSpecificationHandle typeSpecHandle, bool namespaceQualified, string signaturePrefix)
        {
            ValidateHandle(typeSpecHandle, TableIndex.TypeSpec);
            var typeSpec = _metadataReader.GetTypeSpecification(typeSpecHandle);
            var genericContext = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            return typeSpec.DecodeSignature(this, genericContext);
        }

        private string EmitFieldDefinitionName(FieldDefinitionHandle fieldDefHandle, bool namespaceQualified, string? owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(fieldDefHandle, TableIndex.Field);
            var fieldDef = _metadataReader.GetFieldDefinition(fieldDefHandle);
            var genericContext = new DisassemblingGenericContext(Array.Empty<string>(), Array.Empty<string>());
            var output = new StringBuilder();
            output.Append(fieldDef.DecodeSignature(this, genericContext));
            output.Append(' ');
            output.Append(EmitHandleName(fieldDef.GetDeclaringType(), namespaceQualified, owningTypeOverride));
            output.Append('.');
            output.Append(signaturePrefix);
            output.Append(_metadataReader.GetString(fieldDef.Name));
            return output.ToString();
        }

        private string EmitString(StringHandle handle)
        {
            return _metadataReader.GetString(handle);
        }
    }
}
