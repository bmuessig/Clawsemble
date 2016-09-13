using System;
using System.Collections.Generic;
using System.IO;

namespace Clawsemble
{
    public class Binary
    {
        public BinaryType Type;
        public MetaHeader Meta;
        public Dictionary<byte, string> Slots;
        public Dictionary<byte, byte[]> Constants;
        public Dictionary<byte, byte[]> Symbols;

        public Binary()
        {
        }

        public byte[] Bake()
        {
            var memst = new MemoryStream();
            this.Bake(memst);
            byte[] bytes = memst.ToArray();
            memst.Close();

            return bytes;
        }

        public void Bake(Stream Stream)
        {
            
        }
    }
}

