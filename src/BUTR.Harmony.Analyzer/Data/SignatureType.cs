using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;

namespace BUTR.Harmony.Analyzer.Data
{
    internal record SignatureType
    {
        public static SignatureType FromString(string type) => new() { Name = type };
        public static SignatureType FromFunctionPointer(MethodSignature<SignatureType> signature) => new() { Name = string.Empty, IsFunctionPointer = true, FunctionPointerSignature = signature };

        public string Name { get; set; }

        public bool IsRef { get; set; }
        public bool IsPointer { get; set; }
        public bool IsPinned { get; set; }
        public bool IsNested { get; set; }

        public bool IsGeneric { get; set; }
        public ImmutableArray<SignatureType> GenericParameters { get; set; }

        public bool IsArray { get; set; }
        public ArrayShape ArrayShape { get; set; }

        public bool IsFunctionPointer { get; set; }
        public MethodSignature<SignatureType> FunctionPointerSignature { get; set; }

        public override string? ToString() => ToString(false);
        public string? ToString(bool reformatGenerics)
        {
            return IsGeneric
                ? $"{(reformatGenerics ? Generics(Name) : Name)}<{string.Join(",", GenericParameters.Select(x => x.ToString(reformatGenerics)))}>"
                //? $"{(reformatGenerics ? Generics(Name) : Name)}<{string.Join(",", GenericParameters.Select(x => x.ToString(reformatGenerics)))}>"
                //? $"{Name}[{string.Join(",", GenericParameters)}]"
                : IsArray
                    ? GetArray(Name, ArrayShape)
                    : IsFunctionPointer
                        ? GetFunctionPointer(FunctionPointerSignature)
                        : reformatGenerics ? Generics(Name) : Name;
        }

        private static string? Generics(string? str)
        {
            if (str?.IndexOf('`') is { } idx && idx != -1)
            {
                str = str.Substring(0, idx);
            }
            return str;
        }

        private static string GetArray(string? name, ArrayShape shape)
        {
            var builder = new StringBuilder();

            builder.Append(name);
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

        private static string GetFunctionPointer(MethodSignature<SignatureType> signature)
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
    }
}