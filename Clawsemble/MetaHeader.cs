using System;

namespace Clawsemble
{
    public class MetaHeader
    {
        public string Title;
        public string Description;
        public string Author;
        public string Copyright;
        public Version Version;

        public Version MinRuntimeVersion;
        public byte MinVarstackSize;
        public byte MinCallstackSize;
        public byte MinPoolSize;

        public MetaHeader()
        {
            Title = "";
            Description = "";
            Author = "";
            Version = new Version(0, 0, 0);
        }

        public MetaHeader(string Title, string Author, Version Version, string Description = "", string Copyright = "")
        {
            this.Title = Title;
            this.Author = Author;
            this.Version = Version;
            this.Description = Description;
            this.Copyright = Copyright;
        }
    }
}

