using System;
using System.Text;

namespace Clawsemble
{
    public static class Logger
    {
        public static LogPriority Priority { get; set; }

        static Logger()
        {
            Priority = LogPriority.Information;
        }

        public static void Error(string Message, bool AppendLine = false)
        {
            if (Priority > LogPriority.Error)
                return;
            if (AppendLine)
                Console.WriteLine(Indent(Message));
            else
                Console.WriteLine("Error: {0}", Message);
        }

        public static void Warn(string Message, bool AppendLine = false)
        {
            if (Priority > LogPriority.Warning)
                return;
            if (AppendLine)
                Console.WriteLine(Indent(Message));
            else
                Console.WriteLine("Warning: {0}", Message);
        }

        public static void Info(string Message, bool AppendLine = false)
        {
            if (Priority > LogPriority.Information)
                return;

            if (AppendLine)
                Console.WriteLine(Indent(Message));
            else
                Console.WriteLine("{0}", Message);
        }

        public static void ExtInfo(string Message, bool AppendLine = false)
        {
            if (Priority > LogPriority.ExtendedInformation)
                return;

            if (AppendLine)
                Console.WriteLine(Indent(Message));
            else
                Console.WriteLine("{0}", Message);
        }

        public static void Debug(string Message, bool AppendLine = false)
        {
            if (Priority > LogPriority.Debug)
                return;
            if (AppendLine)
                Console.WriteLine(Indent(Message));
            else
                Console.WriteLine("Debug: {0}", Message);
        }

        private static string Indent(string Message)
        {
            string[] lines = Message.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var sb = new StringBuilder();

            int linecounter = 0;
            foreach (string line in lines) {
                sb.AppendFormat("  {0}", line);
                if (linecounter + 1 < lines.Length)
                    sb.AppendLine();
                linecounter++;
            }

            return sb.ToString();
        }
    }
}

