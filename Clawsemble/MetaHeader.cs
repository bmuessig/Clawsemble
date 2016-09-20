using System;
using System.IO;

namespace Clawsemble
{
    public class MetaHeader
    {
        public string Title;
        public string Description;
        public string Author;
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

        public MetaHeader(string Title, string Author, Version Version, string Description = "")
        {
            this.Title = Title;
            this.Author = Author;
            this.Version = Version;
            this.Description = Description;
        }

        public void Bake(Stream Stream)
        {
            // Start with the title
            WriteString(Title, Stream);
            // Now the description
            WriteString(Description, Stream);
            // Now the author
            WriteString(Author, Stream);
            // Continue with the version
            WriteVersion(Version, Stream);

            // Now write the min. runtime version
            WriteVersion(MinRuntimeVersion, Stream);
            // Followed by the min. varstack size
            Stream.WriteByte(MinVarstackSize);
            // Followed by the min. callstack size
            Stream.WriteByte(MinCallstackSize);
            // Followed by the min. pool size
            Stream.WriteByte(MinPoolSize);
        }

        private void WriteString(string String, Stream Stream)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(String);
            Stream.WriteByte((byte)bytes.Length);
            Stream.Write(bytes, 0, bytes.Length);
        }

        private void WriteVersion(Version Version, Stream Stream)
        {
            Stream.WriteByte(Version.Major);
            Stream.WriteByte(Version.Minor);
            Stream.WriteByte(Version.Revision);
        }
    }
}