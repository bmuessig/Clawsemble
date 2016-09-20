using System;

namespace Clawsemble
{
    public static class GlobalConstants
    {
        public static int MaxSlotNameLength { get { return 6; } }
        public static int MaxSlot { get { return 0xF; } }
        public static int MaxSlots { get { return MaxSlot + 1; } }
        public static int MaxSymbol { get { return 0xFE; } }
        public static int MaxSymbols { get { return MaxSymbol + 1; } }
        public static int MaxConstants { get { return 0xFF; } }
        public static byte MaxNativeInstrs { get { return 0x7f; } }
    }
}

