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
        public bool IsIndexSet;

        // Data
        public List<Instruction> Instructions;

        //.ctors
        public Symbol(string Name)
        {
            this.Name = Name;
            this.Index = 0;
            this.IsIndexFixed = false;
            this.IsIndexSet = false;
            this.Instructions = new List<Instruction>();
        }

        public Symbol(string Name, byte Index)
        {
            this.Name = Name;
            this.Index = Index;
            this.IsIndexFixed = true;
            this.IsIndexSet = true;
            this.Instructions = new List<Instruction>();
        }

        // Compile current symbol
        public byte[] Compile(List<Symbol> SymbolTable)
        {
            throw new NotImplementedException();
        }
    }
}

