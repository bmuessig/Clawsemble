using System;

namespace Clawsemble
{
    public struct InstructionPayload
    {
        public Instruction Instruction;
        public byte[] Arguments;
        public string Label;
    }
}

