using System;

namespace Clawsemble
{
    public struct InstructionSignature
    {
        public string Mnemonic { get; private set; }

        public byte Code { get; private set; }

        public bool IsExtended { get; private set; }

        public InstructionArgumentType[] Arguments { get; private set; }

        // Not extended .ctor
        public InstructionSignature(string Mnemoric, byte Code, params InstructionArgumentType[] Arguments)
        {
            this.Mnemonic = Mnemoric;
            this.Code = Code;
            this.IsExtended = false;
            this.Arguments = Arguments;
        }

        // Custom .ctor
        public InstructionSignature(string Mnemoric, byte Code, bool IsExtended, params InstructionArgumentType[] Arguments)
        {
            this.Mnemonic = Mnemoric;
            this.Code = Code;
            this.IsExtended = IsExtended;
            this.Arguments = Arguments;
        }
    }
}
