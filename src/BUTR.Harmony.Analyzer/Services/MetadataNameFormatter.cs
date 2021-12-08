using BUTR.Harmony.Analyzer.Data;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace BUTR.Harmony.Analyzer.Services
{
    /// <summary>
    /// Helper class for converting metadata tokens into their textual representation.
    /// Taken from https://github.com/dotnet/runtime/blob/f179b7634370fc9181610624cc095370ec53e072/src/coreclr/tools/aot/ILCompiler.Reflection.ReadyToRun/ReadyToRunSignature.cs#L70
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class MetadataNameFormatter : DisassemblingTypeProvider
    {
        private readonly MetadataReader _metadataReader;

        private MetadataNameFormatter(MetadataReader metadataReader)
        {
            _metadataReader = metadataReader;
        }

        public static SignatureType FormatHandle(MetadataReader metadataReader, Handle handle, bool namespaceQualified = true, SignatureType? owningTypeOverride = null, string signaturePrefix = "")
        {
            var formatter = new MetadataNameFormatter(metadataReader);
            return formatter.EmitHandleName(handle, namespaceQualified, owningTypeOverride, signaturePrefix);
        }

        private SignatureType EmitHandleName(Handle handle, bool namespaceQualified, SignatureType? owningTypeOverride, string signaturePrefix = "")
        {
            try
            {
                return handle.Kind switch
                {
                    HandleKind.MemberReference => EmitMemberReferenceName((MemberReferenceHandle) handle, owningTypeOverride, signaturePrefix),
                    HandleKind.MethodSpecification => EmitMethodSpecificationName((MethodSpecificationHandle) handle, owningTypeOverride, signaturePrefix),
                    HandleKind.MethodDefinition => EmitMethodDefinitionName((MethodDefinitionHandle) handle, owningTypeOverride, signaturePrefix),
                    HandleKind.TypeReference => EmitTypeReferenceName((TypeReferenceHandle) handle, namespaceQualified, signaturePrefix),
                    HandleKind.TypeSpecification => EmitTypeSpecificationName((TypeSpecificationHandle) handle, namespaceQualified, signaturePrefix),
                    HandleKind.TypeDefinition => EmitTypeDefinitionName((TypeDefinitionHandle) handle, namespaceQualified, signaturePrefix),
                    HandleKind.FieldDefinition => EmitFieldDefinitionName((FieldDefinitionHandle) handle, namespaceQualified, owningTypeOverride, signaturePrefix),
                    _ => throw new NotImplementedException(),
                };
            }
            catch (Exception ex)
            {
                return SignatureType.FromString($"$$INVALID-{handle.Kind}-{MetadataTokens.GetRowNumber((EntityHandle) handle):X6}: {ex.Message}");
            }
        }

        private void ValidateHandle(EntityHandle handle, TableIndex tableIndex)
        {
            var rowid = MetadataTokens.GetRowNumber(handle);
            var tableRowCount = _metadataReader.GetTableRowCount(tableIndex);
            if (rowid <= 0 || rowid > tableRowCount)
            {
                throw new NotImplementedException($"Invalid handle {MetadataTokens.GetToken(handle):X8} in table {tableIndex} ({tableRowCount} rows)");
            }
        }

        private SignatureType EmitMethodSpecificationName(MethodSpecificationHandle methodSpecHandle, SignatureType? owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(methodSpecHandle, TableIndex.MethodSpec);
            var methodSpec = _metadataReader.GetMethodSpecification(methodSpecHandle);
            var type = EmitHandleName(methodSpec.Method, namespaceQualified: true, owningTypeOverride: owningTypeOverride, signaturePrefix: signaturePrefix);
            var signature = methodSpec.DecodeSignature(this, DisassemblingGenericContext.Empty); // No idea
            if (Debugger.IsAttached) Debugger.Break();
            return type;
        }

        private SignatureType EmitMemberReferenceName(MemberReferenceHandle memberRefHandle, SignatureType? owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(memberRefHandle, TableIndex.MemberRef);
            var memberRef = _metadataReader.GetMemberReference(memberRefHandle);
            var builder = new StringBuilder();
            switch (memberRef.GetKind())
            {
                case MemberReferenceKind.Field:
                {
                    var fieldSig = memberRef.DecodeFieldSignature(this, DisassemblingGenericContext.Empty);
                    builder.Append(fieldSig);
                    builder.Append(" ");
                    builder.Append(EmitContainingTypeAndMemberName(memberRef, owningTypeOverride, signaturePrefix));
                    break;
                }

                case MemberReferenceKind.Method:
                {
                    var methodSig = memberRef.DecodeMethodSignature(this, DisassemblingGenericContext.Empty);
                    builder.Append(methodSig.ReturnType);
                    builder.Append(" ");
                    builder.Append(EmitContainingTypeAndMemberName(memberRef, owningTypeOverride, signaturePrefix));
                    builder.Append(EmitMethodSignature(methodSig));
                    break;
                }

                default:
                    throw new NotImplementedException(memberRef.GetKind().ToString());
            }

            return SignatureType.FromString(builder.ToString());
        }

        private SignatureType EmitMethodDefinitionName(MethodDefinitionHandle methodDefinitionHandle, SignatureType? owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(methodDefinitionHandle, TableIndex.MethodDef);
            var methodDef = _metadataReader.GetMethodDefinition(methodDefinitionHandle);
            var methodSig = methodDef.DecodeSignature(this, DisassemblingGenericContext.Empty);
            var builder = new StringBuilder();
            builder.Append(methodSig.ReturnType);
            builder.Append(" ");
            owningTypeOverride ??= EmitHandleName(methodDef.GetDeclaringType(), namespaceQualified: true, owningTypeOverride: null);
            builder.Append(owningTypeOverride);
            builder.Append(".");
            builder.Append(signaturePrefix);
            builder.Append(EmitString(methodDef.Name));
            builder.Append(EmitMethodSignature(methodSig));
            return SignatureType.FromString(builder.ToString());
        }

        private SignatureType EmitMethodSignature(MethodSignature<SignatureType> methodSignature)
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
            return SignatureType.FromString(builder.ToString());
        }

        private SignatureType EmitContainingTypeAndMemberName(MemberReference memberRef, SignatureType? owningTypeOverride, string signaturePrefix)
        {
            owningTypeOverride ??= EmitHandleName(memberRef.Parent, namespaceQualified: true, owningTypeOverride: null);
            if (Debugger.IsAttached) Debugger.Break();
            return owningTypeOverride with { Name = $"{owningTypeOverride.Name}.{signaturePrefix}{EmitString(memberRef.Name)}" }; // No idea what emits this
        }

        private SignatureType EmitTypeReferenceName(TypeReferenceHandle typeRefHandle, bool namespaceQualified, string signaturePrefix)
        {
            ValidateHandle(typeRefHandle, TableIndex.TypeRef);
            var typeRef = _metadataReader.GetTypeReference(typeRefHandle);
            var typeName = EmitString(typeRef.Name);
            var output = "";
            if (typeRef.ResolutionScope.Kind != HandleKind.AssemblyReference)
            {
                // Nested type - format enclosing type followed by the nested type
                var originalType = EmitHandleName(typeRef.ResolutionScope, namespaceQualified, owningTypeOverride: null);
                return originalType with { IsNested = true, Name = $"{originalType.Name}+{typeName}" };
            }
            if (namespaceQualified)
            {
                output = EmitString(typeRef.Namespace);
                if (!string.IsNullOrEmpty(output))
                {
                    output += ".";
                }
            }
            return SignatureType.FromString(output + signaturePrefix + typeName);
        }

        private SignatureType EmitTypeDefinitionName(TypeDefinitionHandle typeDefHandle, bool namespaceQualified, string signaturePrefix)
        {
            ValidateHandle(typeDefHandle, TableIndex.TypeDef);
            var typeDef = _metadataReader.GetTypeDefinition(typeDefHandle);
            var typeName = signaturePrefix + EmitString(typeDef.Name);
            if (typeDef.IsNested)
            {
                // Nested type
                var originalType = EmitHandleName(typeDef.GetDeclaringType(), namespaceQualified, owningTypeOverride: null);
                return originalType with { IsNested = true, Name = $"{originalType.Name}+{typeName}" };
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
            return SignatureType.FromString(output + typeName);
        }

        private SignatureType EmitTypeSpecificationName(TypeSpecificationHandle typeSpecHandle, bool namespaceQualified, string signaturePrefix)
        {
            ValidateHandle(typeSpecHandle, TableIndex.TypeSpec);
            var typeSpec = _metadataReader.GetTypeSpecification(typeSpecHandle);
            return typeSpec.DecodeSignature(this, DisassemblingGenericContext.Empty);
        }

        private SignatureType EmitFieldDefinitionName(FieldDefinitionHandle fieldDefHandle, bool namespaceQualified, SignatureType? owningTypeOverride, string signaturePrefix)
        {
            ValidateHandle(fieldDefHandle, TableIndex.Field);
            var fieldDef = _metadataReader.GetFieldDefinition(fieldDefHandle);
            var output = new StringBuilder();
            output.Append(fieldDef.DecodeSignature(this, DisassemblingGenericContext.Empty));
            output.Append(' ');
            output.Append(EmitHandleName(fieldDef.GetDeclaringType(), namespaceQualified, owningTypeOverride));
            output.Append('.');
            output.Append(signaturePrefix);
            output.Append(_metadataReader.GetString(fieldDef.Name));
            return SignatureType.FromString(output.ToString());
        }

        private string EmitString(StringHandle handle)
        {
            return _metadataReader.GetString(handle);
        }
    }
}