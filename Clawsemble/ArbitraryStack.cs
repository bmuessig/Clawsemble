using System;
using System.Collections.Generic;

namespace Clawsemble
{
    public class ArbitraryStack<T> : Stack<T>
    {
        public ArbitraryStack() : base()
        {
        }

        public void Change(T newItem)
        {
            this.Pop();
            this.Push(newItem);
        }
    }
}

