using System;

namespace Clawsemble
{
    public enum BitbakeErrorType
    {
        TooManyConstants,
        TooManySlots,
        TooManySymbols,
        ConstantLength,
        SymbolLength,
        ConstantOutOfBounds,
        SlotOutOfBounds,
        SymbolOutOfBounds,
        InvalidBinaryType
    }
}

