using System;

namespace Clawsemble
{
    public struct Token
    {
        public TokenType Type;
        public string Content;
        public Constant Constant;
        public bool HasConstant;
        public uint File;
        public uint Line;
        public uint Position;
    }
}

