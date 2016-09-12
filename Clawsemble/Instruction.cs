using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public class Instruction
    {
        public InstructionSignature Signature { get; set; }
        public List<ArgumentToken> Arguments { get; set; }
        public string Label { get; set; }

        public Instruction()
        {
            Signature = new InstructionSignature();
            Arguments = new List<ArgumentToken>();
            Label = "";
        }

        public byte[] Compile(BinaryType Flags)
        {
            return null;
        }
    }
}

