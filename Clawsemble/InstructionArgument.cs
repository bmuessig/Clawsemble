using System;

namespace Clawsemble
{
    public class InstructionArgument
    {
        public InstructionArgumentType Type { get; private set; }
        public InstructionArgumentTarget Target {
            get {
                switch (Type) {
                case InstructionArgumentType.Anything:
                case InstructionArgumentType.Label:
                case InstructionArgumentType.Number:
                    return InstructionArgumentTarget.Number;
                case InstructionArgumentType.UnsignedNumber:
                    return InstructionArgumentTarget.UnsignedNumber;
                default:
                    return InstructionArgumentTarget.Byte;
                }
            }
        }
        public bool AllowConversion { get; private set; }

        public InstructionArgument(InstructionArgumentType Type)
        {
            this.Type = Type;

            switch (Type) { // these types don't allow arbitrary numbers to be used
            case InstructionArgumentType.Array:
            case InstructionArgumentType.ByteArray:
            case InstructionArgumentType.Data:
            case InstructionArgumentType.InternSymbol:
            case InstructionArgumentType.Label:
            case InstructionArgumentType.ShortLabelBw:
            case InstructionArgumentType.ShortLabelFw:
            case InstructionArgumentType.Module:
            case InstructionArgumentType.String:
            case InstructionArgumentType.Values:
                AllowConversion = false;
                break;
            default: // we allow conversion for the rest
                AllowConversion = true;
                break;
            }
        }

        public static bool TryParse(string Value, out InstructionArgument Output, bool IgnoreCase = true)
        {
            InstructionArgumentType type;
            bool result = Enum.TryParse(Value, IgnoreCase, out type);
            if (result) {
                Output = new InstructionArgument(type);
                return true;
            }

            Output = null;
            return false;
        }

        public bool NormalizeArg(ref ArgumentToken Token)
        {
            if (!CheckArg(Token))
                return false;
            
            switch (this.Target) {
            case InstructionArgumentTarget.Number:
            case InstructionArgumentTarget.UnsignedNumber:
                switch (Token.Type) {
                case ArgumentTokenType.ByteValue:
                    Token.Set((long)Token.Number);
                    return true;
                case ArgumentTokenType.ReferenceByte:
                    Token.Set((long)Token.Number, Token.Target);
                    return true;
                case ArgumentTokenType.NumberValue:
                case ArgumentTokenType.ReferenceNumber:
                    return true;
                default:
                    return false;
                }
            case InstructionArgumentTarget.Byte:
                switch (Token.Type) {
                case ArgumentTokenType.ByteValue:
                case ArgumentTokenType.ReferenceByte:
                    return true;
                case ArgumentTokenType.NumberValue:
                    Token.Set((byte)Token.Byte);
                    return true;
                case ArgumentTokenType.ReferenceNumber:
                    if (Token.Target == ReferenceType.Label)
                        Token.Set((byte)Math.Abs(Token.Number), ReferenceType.Label);
                    else
                        Token.Set((byte)Token.Byte, Token.Target);
                    return true;
                default:
                    return false;
                }
            }

            return false;
        }
            
        public bool CheckArg(ArgumentToken CheckToken)
        {
            if (CheckToken.Type == ArgumentTokenType.ByteValue) {
                switch (this.Type) {
                case InstructionArgumentType.Module:
                    if (CheckToken.Byte > GlobalConstants.MaxSlot)
                        return false;
                    return true;
                case InstructionArgumentType.ExternSymbol:
                    if (CheckToken.Byte > GlobalConstants.MaxSymbol)
                        return false;
                    return true;
                case InstructionArgumentType.Anything:
                case InstructionArgumentType.Byte:
                case InstructionArgumentType.Number:
                case InstructionArgumentType.UnsignedNumber:
                    return true;
                default:
                    return false;
                }
            } else if (CheckToken.Type == ArgumentTokenType.NumberValue) {
                switch (this.Type) {
                case InstructionArgumentType.UnsignedNumber:
                    if (CheckToken.Number < 0)
                        return false;
                    return true;
                case InstructionArgumentType.Anything:
                case InstructionArgumentType.Number:
                    return true;
                case InstructionArgumentType.Byte:
                    if (CheckToken.Number < byte.MinValue || CheckToken.Number > byte.MaxValue)
                        return false;
                    return true;
                default:
                    return false;
                }
            } else if (CheckToken.Type == ArgumentTokenType.ReferenceByte) {
                switch (this.Type) {
                case InstructionArgumentType.Anything:
                    return true;
                case InstructionArgumentType.Array:
                    if (CheckToken.Target != ReferenceType.Values && CheckToken.Target != ReferenceType.Data &&
                        CheckToken.Target != ReferenceType.String)
                        return false;
                    return true;
                case InstructionArgumentType.ByteArray:
                    if (CheckToken.Target != ReferenceType.Data && CheckToken.Target != ReferenceType.String)
                        return false;
                    return true;
                case InstructionArgumentType.String:
                    if (CheckToken.Target != ReferenceType.String)
                        return false;
                    return true;
                case InstructionArgumentType.Values:
                    if (CheckToken.Target != ReferenceType.Values)
                        return false;
                    return true;
                case InstructionArgumentType.Data:
                    if (CheckToken.Target != ReferenceType.Data)
                        return false;
                    return true;
                case InstructionArgumentType.InternSymbol:
                    if (CheckToken.Target != ReferenceType.InternSymbol)
                        return false;
                    return true;
                case InstructionArgumentType.ExternSymbol:
                    if (CheckToken.Target != ReferenceType.ExternSymbol)
                        return false;
                    return true;
                case InstructionArgumentType.ShortLabelBw:
                    if (CheckToken.Target != ReferenceType.Label)
                        return false;
                    return true;
                case InstructionArgumentType.ShortLabelFw:
                    if (CheckToken.Target != ReferenceType.Label)
                        return false;
                    return true;
                default:
                    return false;
                }
            } else if (CheckToken.Type == ArgumentTokenType.ReferenceNumber) {
                switch (this.Type) {
                case InstructionArgumentType.Anything:
                    return true;
                case InstructionArgumentType.Label:
                    if (CheckToken.Target != ReferenceType.Label)
                        return false;
                    return true;
                case InstructionArgumentType.ShortLabelBw:
                    if (CheckToken.Target != ReferenceType.Label)
                        return false;
                    if (CheckToken.Number >= 0)
                        return false;
                    if (CheckToken.Number < byte.MinValue)
                        return false;
                    return true;
                case InstructionArgumentType.ShortLabelFw:
                    if (CheckToken.Target != ReferenceType.Label)
                        return false;
                    if (CheckToken.Number <= 0)
                        return false;
                    if (CheckToken.Number > byte.MaxValue)
                        return false;
                    return true;
                default:
                    return false;
                }
            } else
                return false;
        }
            
        public new string ToString()
        {
            return string.Format("{0}{1}", Type.ToString(), AllowConversion ? " (or respective number)" : "");
        }
    }
}

