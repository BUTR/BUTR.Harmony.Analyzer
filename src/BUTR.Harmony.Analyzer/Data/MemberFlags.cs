using System;

namespace BUTR.Harmony.Analyzer.Data
{
    [Flags]
    internal enum MemberFlags
    {
        None = 0,
        Declared = 1,
        Method = 2,
        Field = 4,
        Property = 8,
        Getter = 16,
        Setter = 32,
        Constructor = 64,
        StaticConstructor = 128,
        Delegate = 256,
    }
}