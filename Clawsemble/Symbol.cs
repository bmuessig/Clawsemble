using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public class Symbol
    {
        // Meta
        public byte Index { get; private set; }
        public bool IsIndexFixed { get; private set; }
        public bool IsIndexSet { get; private set; }

        // Data
        public List<Instruction> Instructions;

        //.ctors
        public Symbol()
        {
            this.Index = 0;
            this.IsIndexFixed = false;
            this.IsIndexSet = false;
            this.Instructions = new List<Instruction>();
        }

        public Symbol(byte Index)
        {
            this.Index = Index;
            this.IsIndexFixed = true;
            this.IsIndexSet = true;
            this.Instructions = new List<Instruction>();
        }

        public void SetIndex(byte Index)
        {
            IsIndexSet = true;
            this.Index = Index;
        }

        // Compile current symbol
        public byte[] Compile(List<Symbol> SymbolTable)
        {
            throw new NotImplementedException();
        }
    }
}

