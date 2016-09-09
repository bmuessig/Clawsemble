using System;

namespace Clawsemble
{
    public struct ExecutableVersion
    {
        public byte Major;
        public byte Minor;
        public byte Revision;

        public ExecutableVersion(byte Major, byte Minor, byte Revision = 0)
        {
            this.Major = Major;
            this.Minor = Minor;
            this.Revision = Revision;
        }
    }
}

