using System;

namespace Clawsemble
{
    public struct CompilerToken
    {
        public byte IndexRef { get; private set; }
        public string NameRef { get; private set; }
        public byte Byte { get; private set; }
        public long Value { get; private set; }
        public CompilerTokenType Type { get; private set; }

        /*public CompilationToken(long Value)
        {
            this.Value = Value;
            Type = CompilationTokenType.Value;
        }

        public CompilationToken(string NameRef)
        {
            this.NameRef = NameRef;

        }*/
    }
}

