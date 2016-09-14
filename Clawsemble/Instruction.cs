using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public class Instruction
    {
        public InstructionSignature Signature { get; set; }
        public List<ArgumentToken> Arguments { get; set; }
        public string Label { get; set; }

        public uint File { get; set; }
        public uint Line { get; set; }
        public uint Position{ get; set; }

        public Instruction(uint Position = 0, uint Line = 0, uint File = 0)
        {
            Signature = new InstructionSignature();
            Arguments = new List<ArgumentToken>();
            Label = "";

            this.Position = Position;
            this.Line = Line;
            this.File = File;
        }

        public byte GetSize(byte BytesPerLong)
        {
            byte size = 1; // covers the instruction itelf already

            foreach (InstructionArgumentType arg in Signature.Arguments) {
                switch (arg) {
                // let's just have all the other values here
                case InstructionArgumentType.Number:
                case InstructionArgumentType.Label:
                    size += BytesPerLong;
                    break;
                default: // default covers all byte values
                    size++;
                    break;
                }
            }

            return size;
        }

        public byte[] Compile(BinaryType Flags)
        {
            return null;
        }
    }
}

