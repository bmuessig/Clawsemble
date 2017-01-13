using System;

namespace Clawsemble
{
    public struct InstructionSignature
    {
        public string Mnemonic { get; private set; }

        public byte Code { get; private set; }

        public bool IsExtended { get; private set; }

        public InstructionArgument[] Arguments { get; private set; }

        // Not extended .ctor
        public InstructionSignature(string Mnemonic, byte Code, params InstructionArgument[] Arguments)
        {
            this = default(InstructionSignature);
            this.Mnemonic = Mnemonic;
            this.Code = Code;
            this.IsExtended = false;
            this.Arguments = Arguments;
        }

        // Custom .ctor
        public InstructionSignature(string Mnemonic, byte Code, bool IsExtended, params InstructionArgument[] Arguments)
        {
            this = default(InstructionSignature);
            this.Mnemonic = Mnemonic;
            this.Code = Code;
            this.IsExtended = IsExtended;
            this.Arguments = Arguments;
        }
    }
}
