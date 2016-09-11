using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public struct NativeInstruction : IInstruction
    {
        // Custom
        public InstructionSignature Instruction  { get; set; }

        // IInstruction
        public List<long> Arguments { get; set; }
        public string Label { get; set; }

        public bool Validate()
        {
            return true;
        }

        public byte[] Compile(BinaryType Flags)
        {
            return null;
        }
    }
}

