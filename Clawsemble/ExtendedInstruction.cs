using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public class ExtendedInstruction : IInstruction
    {
        public ExtendedInstruction()
        {
        }

        // IInstruction
        public List<long> Arguments { get; set; }
        public string Label { get; set; }

        public bool Validate()
        {
            return true;
        }

        public byte[] Compile(BinaryType Flags)
        {
            return null;
        }
    }
}

