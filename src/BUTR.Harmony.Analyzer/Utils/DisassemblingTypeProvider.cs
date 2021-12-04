using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text;

namespace BUTR.Harmony.Analyzer.Utils
{
    [ExcludeFromCodeCoverage]
    public class DisassemblingGenericContext
    {
        public string[] MethodParameters { get; }
        public string[] TypeParameters { get; }

        public DisassemblingGenericContext(string[]? typeParameters, string[]? methodParameters)
        {
            MethodParameters = methodParameters ?? Array.Empty<string>();
            TypeParameters = typeParameters ?? Array.Empty<string>(); ;
        }

    }

    [ExcludeFromCodeCoverage]
    public abstract class StringTypeProviderBase<TGenericContext> : ISignatureTypeProvider<string, TGenericContext>
    {
        public virtual string GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
        {
            PrimitiveTypeCode.Void => typeof(void).FullName,
            PrimitiveTypeCode.Boolean => typeof(bool).FullName,
            PrimitiveTypeCode.Char => typeof(char).FullName,
            PrimitiveTypeCode.SByte => typeof(sbyte).FullName,
            PrimitiveTypeCode.Byte => typeof(byte).FullName,
            PrimitiveTypeCode.Int16 => typeof(short).FullName,
            PrimitiveTypeCode.UInt16 => typeof(ushort).FullName,
            PrimitiveTypeCode.Int32 => typeof(int).FullName,
            PrimitiveTypeCode.UInt32 => typeof(uint).FullName,
            PrimitiveTypeCode.Int64 => typeof(long).FullName,
            PrimitiveTypeCode.UInt64 => typeof(ulong).FullName,
            PrimitiveTypeCode.Single => typeof(float).FullName,
            PrimitiveTypeCode.Double => typeof(double).FullName,
            PrimitiveTypeCode.String => typeof(string).FullName,
            PrimitiveTypeCode.TypedReference => "typedbyref",
            PrimitiveTypeCode.IntPtr => typeof(IntPtr).FullName,
            PrimitiveTypeCode.UIntPtr => typeof(UIntPtr).FullName,
            PrimitiveTypeCode.Object => typeof(object).FullName,
            _ => throw new NotImplementedException()
        };

        public virtual string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind = 0)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }

        public virtual string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind = 0)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }

        public virtual string GetSZArrayType(string elementType)
        {
            return $"{elementType}[]";
        }

        public virtual string GetPointerType(string elementType)
        {
            return $"{elementType}*";
        }

        public virtual string GetByReferenceType(string elementType)
        {
            return $"ref {elementType}";
        }

        public virtual string GetPinnedType(string elementType)
        {
            return $"{elementType} pinned";
        }

        public virtual string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
        {
            return $"{genericType}<{string.Join(",", typeArguments)}>";
        }

        public virtual string GetArrayType(string elementType, ArrayShape shape)
        {
            var builder = new StringBuilder();

            builder.Append(elementType);
            builder.Append('[');

            for (var i = 0; i < shape.Rank; i++)
            {
                var lowerBound = 0;

                if (i < shape.LowerBounds.Length)
                {
                    lowerBound = shape.LowerBounds[i];
                    builder.Append(lowerBound);
                }

                builder.Append("...");

                if (i < shape.Sizes.Length)
                {
                    builder.Append(lowerBound + shape.Sizes[i] - 1);
                }

                if (i < shape.Rank - 1)
                {
                    builder.Append(',');
                }
            }

            builder.Append(']');

            return builder.ToString();
        }

        public virtual string GetModifiedType(string modifierType, string unmodifiedType, bool isRequired)
        {
            return $"{unmodifiedType}{(isRequired ? " modreq(" : " modopt(")}{modifierType})";
        }

        public virtual string GetFunctionPointerType(MethodSignature<string> signature)
        {
            var parameterTypes = signature.ParameterTypes;

            var requiredParameterCount = signature.RequiredParameterCount;

            var builder = new StringBuilder();
            builder.Append("method ");
            builder.Append(signature.ReturnType);
            builder.Append(" *(");

            int i;
            for (i = 0; i < requiredParameterCount; i++)
            {
                builder.Append(parameterTypes[i]);
                if (i < parameterTypes.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            if (i < parameterTypes.Length)
            {
                builder.Append("..., ");
                for (; i < parameterTypes.Length; i++)
                {
                    builder.Append(parameterTypes[i]);
                    if (i < parameterTypes.Length - 1)
                    {
                        builder.Append(", ");
                    }
                }
            }

            builder.Append(')');
            return builder.ToString();
        }

        public abstract string GetGenericMethodParameter(TGenericContext genericContext, int index);
        public abstract string GetGenericTypeParameter(TGenericContext genericContext, int index);
        public abstract string GetTypeFromSpecification(MetadataReader reader, TGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind);
    }

    // Test implementation of ISignatureTypeProvider<TType, TGenericContext> that uses strings in ilasm syntax as TType.
    // A real provider in any sort of perf constraints would not want to allocate strings freely like this, but it keeps test code simple.
    [ExcludeFromCodeCoverage]
    public class DisassemblingTypeProvider : StringTypeProviderBase<DisassemblingGenericContext>
    {
        public override string GetGenericMethodParameter(DisassemblingGenericContext genericContext, int index)
        {
            if (index >= genericContext.MethodParameters.Length)
            {
                return $"!!{index}";
            }
            return genericContext.MethodParameters[index];
        }

        public override string GetGenericTypeParameter(DisassemblingGenericContext genericContext, int index)
        {
            if (index >= genericContext.TypeParameters.Length)
            {
                return $"!{index}";
            }
            return genericContext.TypeParameters[index];
        }

        public override string GetTypeFromSpecification(MetadataReader reader, DisassemblingGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }

        public string GetTypeFromHandle(MetadataReader reader, DisassemblingGenericContext genericContext, EntityHandle handle)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }
    }
}