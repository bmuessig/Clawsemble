using System;

namespace Clawsemble
{
    public enum InstructionArgumentType
    {

        /* *
         * Byte, Data, String, Function => uint8_t
         * Number => uintXY_t
         * */
        Byte = 0x1,
        String = 0x2,
        Data = 0x4,
        Function = 0x8,
        Number = 0x1F,
        Array = String & Data
    }
}

