using System;

namespace Clawsemble
{
    public class NamedReference
    {
        public string Name { get; private set; }
        public byte Value { get; set; }
        public ReferenceType Type { get; private set; }

        public NamedReference(string Name, ReferenceType Type, byte Value = 0)
        {
            this.Name = Name;
            this.Value = Value;
            this.Type = Type;
        }
    }
}

