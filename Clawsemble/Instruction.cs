using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public struct Instruction
    {
        public InstructionSignature Signature { get; set; }
        public List<long> Arguments { get; set; }
        public string Label { get; set; }

        public byte[] Compile(BinaryType Flags)
        {
            return null;
        }
    }
}

