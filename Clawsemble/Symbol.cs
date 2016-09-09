using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public struct Symbol
    {
        // Meta
        public string Name;
        public byte Index;
        public bool IsIndexFixed;

        // Data
        public List<InstructionPayload> Instructions;

        //.ctor
        public Symbol(string Name)
        {
            this.Name = Name;
            this.Index = 0;
            this.IsIndexFixed = false;
            this.Instructions = new List<InstructionPayload>();
        }

        public Symbol(string Name, byte Index)
        {
            this.Name = Name;
            this.Index = Index;
            this.IsIndexFixed = true;
            this.Instructions = new List<InstructionPayload>();
        }
    }
}

