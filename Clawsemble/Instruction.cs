using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public struct Instruction
    {
        public List<long> Arguments { get; set; }
        public string Label { get; set; }
        public InstructionSignature Signature { get; set; }

        public byte[] Compile(BinaryType Flags)
        {
            return null;
        }
    }
}

