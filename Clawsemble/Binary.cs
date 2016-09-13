using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public class Binary
    {
        BinaryType Type;
        MetaHeader Meta;
        Dictionary<byte, string> Slots;
        Dictionary<byte, byte[]> Constants;
        Dictionary<byte, byte[]> Symbols;

        public Binary()
        {
        }

        public byte[] Bake()
        {
            return null;
        }
    }
}

