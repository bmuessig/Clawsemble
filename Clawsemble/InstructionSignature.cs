using System;

namespace Clawsemble
{
    public struct InstructionSignature
    {
        public string Mnemonic { get; private set; }

        public byte Code { get; private set; }

        public InstructionArgumentType[] Arguments { get; private set; }

        public InstructionSignature(string Mnemoric, byte Code, params InstructionArgumentType[] Arguments)
        {
            this.Mnemonic = Mnemoric;
            this.Code = Code;
            this.Arguments = Arguments;
        }
    }
}
