using System;

namespace Clawsemble
{
    public class ArgumentToken
    {
        public long Number { get; private set; }
        public byte Byte { get; private set; }
        public string String { get; private set; }
        public ArgumentTokenType Type { get; private set; }
        public ReferenceType Target { get; private set; }

        public uint File { get; set; }
        public uint Line { get; set; }
        public uint Position{ get; set; }

        public ArgumentToken(byte Value, uint Position = 0, uint Line = 0, uint File = 0)
        {
            this.Number = Value;
            this.Type = ArgumentTokenType.ByteValue;
            this.Position = Position;
            this.Line = Line;
            this.File = File;
        }

        public ArgumentToken(long Value, uint Position = 0, uint Line = 0, uint File = 0)
        {
            this.Number = Value;
            this.Type = ArgumentTokenType.NumberValue;
            this.Position = Position;
            this.Line = Line;
            this.File = File;
        }

        public ArgumentToken(byte Reference, ReferenceType Target, uint Position = 0, uint Line = 0, uint File = 0)
        {
            this.Byte = Reference;
            this.Type = ArgumentTokenType.ReferenceByte;
            this.Position = Position;
            this.Line = Line;
            this.File = File;
        }

        public ArgumentToken(long Reference, ReferenceType Target, uint Position = 0, uint Line = 0, uint File = 0)
        {
            this.Number = Reference;
            this.Type = ArgumentTokenType.ReferenceNumber;
            this.Position = Position;
            this.Line = Line;
            this.File = File;
        }
            
        public ArgumentToken(string Reference, uint Position = 0, uint Line = 0, uint File = 0)
        {
            this.String = Reference;
            this.Type = ArgumentTokenType.ReferenceString;
            this.Position = Position;
            this.Line = Line;
            this.File = File;
        }

        public void Set(long Value)
        {
            this.Number = Value;
            this.Byte = 0;
            this.String = "";
            this.Type = ArgumentTokenType.NumberValue;
        }

        public void Set(byte Value)
        {
            this.Byte = Value;
            this.Number = 0;
            this.String = "";
            this.Type = ArgumentTokenType.ByteValue;
        }

        public void Set(byte Reference, ReferenceType Target)
        {
            this.Byte = Reference;
            this.Number = 0;
            this.String = "";
            this.Type = ArgumentTokenType.ReferenceByte;
        }

        public void Set(long Reference, ReferenceType Target)
        {
            this.Number = Reference;
            this.Byte = 0;
            this.String = "";
            this.Type = ArgumentTokenType.ReferenceNumber;
        }

        public void Set(string Reference)
        {
            this.String = Reference;
            this.Byte = 0;
            this.Number = 0;
            this.Type = ArgumentTokenType.ReferenceString;
        }

    }
}

