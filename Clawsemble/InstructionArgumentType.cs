using System;

namespace Clawsemble
{
    public enum InstructionArgumentType
    {
        Number = 0x0,
        Label = Number | (0x1 << 1),

        Byte = 0x1,
        ShortLabelFw = Byte | (0x1 << 1),
        ShortLabelBw = Byte | (0x1 << 2),
        Data = Byte | (0x1 << 3),
        Values = Byte | (0x1 << 4),
        String = Byte | (0x1 << 5),
        Symbol = Byte | (0x1 << 6),

        Array = Data | Values | String,

        /* *
          * s16/s32/s64 bit numbers:
          *  * simple value    (0b0000000)
          *  * label offset    (0b0000010)
          * u8 bit number
          *  * simple value    (0b0000001)
          *  * label fw offset (0b0000011)
          *  * label bw offset (0b0000101)
          *  * data            (0b0001001)
          *  * values          (0b0010001)
          *  * string          (0b0100001)
          *  * function        (0b1000001)
          * */
    }
}

