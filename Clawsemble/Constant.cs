using System;

namespace Clawsemble
{
    public class Constant
    {
        public readonly long Number;
        /*public readonly byte Byte;
        public readonly bool ValidByte;*/
        public readonly string String;

        public readonly long[] Array;
        public readonly byte[] ByteArray;
        public readonly ConstantType Type;

        public Constant()
        {
            Type = ConstantType.Empty;
            //ValidByte = false;
        }

        public Constant(string String)
        {
            this.String = String;
            Type = ConstantType.String;
            //ValidByte = false;
        }

        public Constant(long Number)
        {
            this.Number = Number;
            Type = ConstantType.Numeric;
            /*if (Number >= byte.MinValue && Number <= byte.MaxValue) {
                Byte = (byte)Number;
                ValidByte = true;
            } else
                ValidByte = false;*/
        }
        /*
        public Constant(byte Number)
        {
            this.Number = Number;
            this.Byte = Number;
            Type = ConstantType.Numeric;
            ValidByte = true;
        }
*/
        public Constant(Token Token)
            : this(Token.Type, Token.Content)
        {
        }

        public Constant(TokenType Type, string Content)
        {
            if (Type == TokenType.String) {
                String = Content;
                this.Type = ConstantType.String;
                //              ValidByte = false;
            } else if (Type == TokenType.Number) {
                try {
                    Number = Convert.ToInt64(Content);
                    this.Type = ConstantType.Numeric;
                    /*                if (Number >= byte.MinValue && Number <= byte.MaxValue) {
                        Byte = (byte)Number;
                        ValidByte = true;
                    } else
                        ValidByte = false;*/
                } catch (Exception) {
                    throw new Exception("Invalid number!");
                }
            } else if (Type == TokenType.HexadecimalEscape) {
                if (Content.Length > 0 && Content.Length <= 8) {
                    try {
                        Number = Convert.ToInt64(Content, 16);
                        this.Type = ConstantType.Numeric;
                        /*   if (Number >= byte.MinValue && Number <= byte.MaxValue) {
                            Byte = (byte)Number;
                            ValidByte = true;
                        } else
                            ValidByte = false;*/
                    } catch (Exception) {
                        throw new Exception("Invalid hexadecimal number!");
                    }
                } else
                    throw new Exception("Hexadecimal escape size missmatch!");
            } else if (Type == TokenType.Character) {
                if (Content.Length == 1) {
                    Number = (char)Content[0];
                    this.Type = ConstantType.Numeric;
                    //                   Byte = (byte)Number;
                    //ValidByte = true;
                } else
                    throw new Exception("Invalid character size!");
            } else if (Type == TokenType.Empty) {
                this.Type = ConstantType.Empty;
                //ValidByte = false;
            } else
                throw new Exception("Invalid token!");
        }

        public static bool TryParse(Token Token, out Constant Output)
        {
            try {
                Output = new Constant(Token);
            } catch (Exception) {
                Output = null;
                return false;
            }
            return true;
        }

    }
}

