using System;

namespace Clawsemble
{
    public class BitbakeError : Exception
    {
        public BitbakeErrorType Type;
        public bool HasItem;
        public int Item;

        public BitbakeError(BitbakeErrorType Type)
        {
            this.Type = Type;
            HasItem = false;
            Item = 0;
        }

        public BitbakeError(BitbakeErrorType Type, int Item)
        {
            this.Type = Type;
            this.HasItem = true;
            this.Item = Item;
        }

        public new string Message {
            get {
                if (HasItem)
                    return string.Format("{0} at Item #{1}", Type.ToString(), Item);
                else
                    return string.Format("{0}", Type.ToString());
            }
        }
    }
}

