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
        String = Byte | (0x1 << 3),
        Function = Byte | (0x1 << 4),

        Array = Data | String,

        /* *
          * s16/s32/s64 bit numbers:
          *  * simple value (0b00000)
          *  * label offset (0b00010)
          * u8 bit number
          *  * simple value (0b00001)
          *  * label offset (0b00011)
          *  * data         (0b00101)
          *  * string       (0b01001)
          *  * function     (0b10001)
          * */
    }
}

