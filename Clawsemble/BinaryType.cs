using System;

namespace Clawsemble
{
    public enum BinaryType
    {
        Executable = 0x0,
        Library = 0x1,
        Bits8 = 0x0,
        Bits16 = 0x2,
        Bits32 = 0x4,
        Bits64 = 0x6,
        Type = 0x1,
        Bits = 0x6
    }
}

