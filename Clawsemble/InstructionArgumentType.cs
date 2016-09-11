using System;

namespace Clawsemble
{
    public enum InstructionArgumentType
    {
        Number = 0x0,
        Label = Number | (0x1 << 1),

        Byte = 0x1,
        ShortLabel = Byte | (0x1 << 1),
        Data = Byte | (0x1 << 2),
        Values = Byte | (0x1 << 3),
        String = Byte | (0x1 << 4),
        Function = Byte | (0x1 << 5),

        Array = Data | Values | String,

        /* *
          * s16/s32/s64 bit numbers:
          *  * simple value (0b000000)
          *  * label offset (0b000010)
          * u8 bit number
          *  * simple value (0b000001)
          *  * label offset (0b000011)
          *  * data         (0b000101)
          *  * values       (0b001001)
          *  * string       (0b010001)
          *  * function     (0b100001)
          * */
    }
}

