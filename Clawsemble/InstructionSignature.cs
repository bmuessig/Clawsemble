using System;

namespace Clawsemble
{
    public struct InstructionSignature
    {
        public string Mnemoric { get; private set; }

        public byte Code { get; private set; }

        public InstructionArgumentType[] Arguments { get; private set; }

        public InstructionSignature(string Mnemoric, byte Code, params InstructionArgumentType[] Arguments)
        {
            this.Mnemoric = Mnemoric;
            this.Code = Code;
            this.Arguments = Arguments;
        }
    }
}
