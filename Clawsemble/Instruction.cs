using System;

namespace Clawsemble
{
    public struct Instruction
    {
        public string Mnemonic { get; private set; }

        public byte Code { get; private set; }

        public InstructionArgumentType[] Arguments { get; private set; }

        public Instruction(string Mnemoric, byte Code, params InstructionArgumentType[] Arguments)
        {
            this.Mnemonic = Mnemoric;
            this.Code = Code;
            this.Arguments = Arguments;
        }
    }
}
