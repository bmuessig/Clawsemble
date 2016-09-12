using System;

namespace Clawsemble
{
    public enum CompilationTokenType
    {
        // Skip/Error
        Empty,
        // String reference
        String,
        // Array reference
        Array,
        // Function reference
        Function,
        // Label
        Label,
        // 64/32/16-bit value
        Value,
        // Byte value
        Byte
    }
}

