using System;

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
                Console.WriteLine(Message);
            else
                Console.WriteLine("Error: {0}", Message);
        }

        public static void Warn(string Message, bool AppendLine = false)
        {
            if (Priority > LogPriority.Warning)
                return;
            if (AppendLine)
                Console.WriteLine(Message);
            else
                Console.WriteLine("Warning: {0}", Message);
        }

        public static void Info(string Message, bool AppendLine = false)
        {
            if (Priority > LogPriority.Information)
                return;

            // this looks stupid, but is just there for easier future expandability
            if (AppendLine)
                Console.WriteLine(Message);
            else
                Console.WriteLine("{0}", Message);
        }

        public static void ExtInfo(string Message, bool AppendLine = false)
        {
            if (Priority > LogPriority.ExtendedInformation)
                return;

            // this looks stupid, but is just there for easier future expandability
            if (AppendLine)
                Console.WriteLine(Message);
            else
                Console.WriteLine("{0}", Message);
        }

        public static void Debug(string Message, bool AppendLine = false)
        {
            if (Priority > LogPriority.Debug)
                return;
            if (AppendLine)
                Console.WriteLine(Message);
            else
                Console.WriteLine("Debug: {0}", Message);
        }
    }
}

