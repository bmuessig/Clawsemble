using System;

namespace Clawsemble
{
    public class ArgumentToken
    {
        public long Number { get; private set; }
        public string String { get; private set; }
        public ArgumentTokenType Type { get; private set; }
        public ReferenceType Target { get; private set; }

        public uint File { get; set; }
        public uint Line { get; set; }
        public uint Position{ get; set; }

        public ArgumentToken(long Value, uint Position = 0, uint Line = 0, uint File = 0)
        {
            this.Number = Value;
            this.Type = ArgumentTokenType.Value;
            this.Position = Position;
            this.Line = Line;
            this.File = File;
        }

        public ArgumentToken(byte Reference, ReferenceType Target, uint Position = 0, uint Line = 0, uint File = 0)
        {
            this.Number = Reference;
            this.Type = ArgumentTokenType.ReferenceNum;
            this.Position = Position;
            this.Line = Line;
            this.File = File;
        }
            
        public ArgumentToken(string Reference, uint Position = 0, uint Line = 0, uint File = 0)
        {
            this.String = Reference;
            this.Type = ArgumentTokenType.ReferenceStr;
            this.Position = Position;
            this.Line = Line;
            this.File = File;
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

