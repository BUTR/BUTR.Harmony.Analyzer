using BUTR.Harmony.Analyzer.Data;

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;

// Taken from https://github.com/dotnet/runtime/blob/f179b7634370fc9181610624cc095370ec53e072/src/coreclr/tools/aot/ILCompiler.Reflection.ReadyToRun/DisassemblingTypeProvider.cs
namespace BUTR.Harmony.Analyzer.Services
{
    [ExcludeFromCodeCoverage]
    internal class DisassemblingGenericContext
    {
        public static DisassemblingGenericContext Empty { get; } = new(Array.Empty<string>(), Array.Empty<string>());

        public string[] MethodParameters { get; }
        public string[] TypeParameters { get; }

        public DisassemblingGenericContext(string[]? typeParameters, string[]? methodParameters)
        {
            MethodParameters = methodParameters ?? Array.Empty<string>();
            TypeParameters = typeParameters ?? Array.Empty<string>();
        }
    }

    [ExcludeFromCodeCoverage]
    internal abstract class StringTypeProviderBase<TGenericContext> : ISignatureTypeProvider<SignatureType, TGenericContext>
    {
        public virtual SignatureType GetPrimitiveType(PrimitiveTypeCode typeCode) => typeCode switch
        {
            PrimitiveTypeCode.Void => SignatureType.FromString(typeof(void).FullName),
            PrimitiveTypeCode.Boolean => SignatureType.FromString(typeof(bool).FullName),
            PrimitiveTypeCode.Char => SignatureType.FromString(typeof(char).FullName),
            PrimitiveTypeCode.SByte => SignatureType.FromString(typeof(sbyte).FullName),
            PrimitiveTypeCode.Byte => SignatureType.FromString(typeof(byte).FullName),
            PrimitiveTypeCode.Int16 => SignatureType.FromString(typeof(short).FullName),
            PrimitiveTypeCode.UInt16 => SignatureType.FromString(typeof(ushort).FullName),
            PrimitiveTypeCode.Int32 => SignatureType.FromString(typeof(int).FullName),
            PrimitiveTypeCode.UInt32 => SignatureType.FromString(typeof(uint).FullName),
            PrimitiveTypeCode.Int64 => SignatureType.FromString(typeof(long).FullName),
            PrimitiveTypeCode.UInt64 => SignatureType.FromString(typeof(ulong).FullName),
            PrimitiveTypeCode.Single => SignatureType.FromString(typeof(float).FullName),
            PrimitiveTypeCode.Double => SignatureType.FromString(typeof(double).FullName),
            PrimitiveTypeCode.String => SignatureType.FromString(typeof(string).FullName),
            PrimitiveTypeCode.TypedReference => SignatureType.FromString("typedbyref"),
            PrimitiveTypeCode.IntPtr => SignatureType.FromString(typeof(IntPtr).FullName),
            PrimitiveTypeCode.UIntPtr => SignatureType.FromString(typeof(UIntPtr).FullName),
            PrimitiveTypeCode.Object => SignatureType.FromString(typeof(object).FullName),
            _ => throw new NotImplementedException()
        };

        public virtual SignatureType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind = 0)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }

        public virtual SignatureType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind = 0)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }

        public virtual SignatureType GetSZArrayType(SignatureType elementType)
        {
            return elementType with { IsArray = true };
        }

        public virtual SignatureType GetPointerType(SignatureType elementType)
        {
            return elementType with { IsPointer = true };
        }

        public virtual SignatureType GetByReferenceType(SignatureType elementType)
        {
            return elementType with { IsRef = true };
        }

        public virtual SignatureType GetPinnedType(SignatureType elementType)
        {
            return elementType with { IsPinned = true };
        }

        public virtual SignatureType GetGenericInstantiation(SignatureType genericType, ImmutableArray<SignatureType> typeArguments)
        {
            return genericType with { IsGeneric = true, GenericParameters = typeArguments };
            //return $"{genericType}<{string.Join(",", typeArguments)}>";
            //return $"{genericType}[{string.Join(",", typeArguments)}]";
        }

        public virtual SignatureType GetArrayType(SignatureType elementType, ArrayShape shape)
        {
            return elementType with { IsArray = true, ArrayShape = shape };
        }

        public virtual SignatureType GetModifiedType(SignatureType modifierType, SignatureType unmodifiedType, bool isRequired)
        {
            return unmodifiedType;
            //return $"{unmodifiedType}{(isRequired ? " modreq(" : " modopt(")}{modifierType})";
        }

        public virtual SignatureType GetFunctionPointerType(MethodSignature<SignatureType> signature)
        {
            return SignatureType.FromFunctionPointer(signature);
        }

        public abstract SignatureType GetGenericMethodParameter(TGenericContext genericContext, int index);
        public abstract SignatureType GetGenericTypeParameter(TGenericContext genericContext, int index);
        public abstract SignatureType GetTypeFromSpecification(MetadataReader reader, TGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind);
    }

    // Test implementation of ISignatureTypeProvider<TType, TGenericContext> that uses strings in ilasm syntax as TType.
    // A real provider in any sort of perf constraints would not want to allocate strings freely like this, but it keeps test code simple.
    [ExcludeFromCodeCoverage]
    internal class DisassemblingTypeProvider : StringTypeProviderBase<DisassemblingGenericContext>
    {
        public override SignatureType GetGenericMethodParameter(DisassemblingGenericContext genericContext, int index)
        {
            if (index >= genericContext.MethodParameters.Length)
            {
                return SignatureType.FromString($"!!{index}");
            }
            return SignatureType.FromString(genericContext.MethodParameters[index]);
        }

        public override SignatureType GetGenericTypeParameter(DisassemblingGenericContext genericContext, int index)
        {
            if (index >= genericContext.TypeParameters.Length)
            {
                return SignatureType.FromString($"!{index}");
            }
            return SignatureType.FromString(genericContext.TypeParameters[index]);
        }

        public override SignatureType GetTypeFromSpecification(MetadataReader reader, DisassemblingGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }

        public SignatureType GetTypeFromHandle(MetadataReader reader, DisassemblingGenericContext genericContext, EntityHandle handle)
        {
            return MetadataNameFormatter.FormatHandle(reader, handle);
        }
    }
}