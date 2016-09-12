using System;

namespace Clawsemble
{
    public class ArgumentToken
    {
        public long Number { get; private set; }
        public string String { get; private set; }
        public ArgumentTokenType Type { get; private set; }
        public ReferenceType Target { get; private set; }

        public ArgumentToken(long Value)
        {
            this.Number = Value;
            this.Type = ArgumentTokenType.Value;
        }

        public ArgumentToken(byte Reference, ReferenceType Target)
        {
            this.Number = Reference;
            this.Type = ArgumentTokenType.ReferenceNum;
        }

        public ArgumentToken(string Reference)
        {
            this.String = Reference;
            this.Type = ArgumentTokenType.ReferenceStr;
        }

        public void Set(long Value)
        {
            this.Number = Value;
            this.Type = ArgumentTokenType.Value;
        }

        public void Set(byte Reference, ReferenceType Target)
        {
            this.Number = Reference;
            this.Type = ArgumentTokenType.ReferenceNum;
        }

        public void Set(string Reference)
        {
            this.String = Reference;
            this.Type = ArgumentTokenType.ReferenceStr;
        }

    }
}

