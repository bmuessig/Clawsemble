using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public interface IInstruction
    {
        List<long> Arguments { get; set; }
        string Label { get; set; }

        bool Validate();
        byte[] Compile(BinaryType Flags);
    }
}

