using System;

namespace Clawsemble
{
    public struct Token
    {
        public TokenType Type;
        public string Content;
        public uint File;
        public uint Line;
    }
}

