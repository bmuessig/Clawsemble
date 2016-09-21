using System;

namespace Clawsemble
{
    public enum InstructionArgumentType
    {
        // The output type is determined by the highest possible argument
        // Values lower that than will simply be converted
        // Numbers as arguments can be disabled or enabled

        // Allow anything (a number will be written)
        Anything,
        // Allow number and byte constants (a number will be written)
        Number,
        // Allow unsigned number and byte constants (an unsigned number will be written)
        UnsignedNumber,
        // Just allow byte constants (a byte will be written)
        Byte,
        // Mutually exclusive, type number
        Label,
        // Mutually exclusive, type byte
        ShortLabelFw,
        // Mutually exclusive, type byte
        ShortLabelBw,
        // Mutually exclusive, type byte
        InternSymbol,
        // Mutually exclusive, type byte
        ExternSymbol,
        // Mutually exclusive, type byte
        Module,
        // Only non-exclusive with values and string, type byte
        Data,
        // Only non-exclusive with data and string, type byte
        Values,
        // Only non-exclusive with data and values, type byte
        String,
        // Data + Values + String, type byte
        Array,
        // Data + String
        ByteArray,
    }
}

