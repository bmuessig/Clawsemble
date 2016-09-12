using System;

namespace Clawsemble
{
    public class NamedReference
    {
        public string Name { get; set; }
        public byte Value { get; set; }
        public ReferenceType Type { get; private set; }

        public NamedReference(ReferenceType Type)
        {
            this.Type = Type;
        }

        public NamedReference(string Name, ReferenceType Type, byte Value = 0)
        {
            this.Name = Name;
            this.Value = Value;
            this.Type = Type;
        }
    }
}

